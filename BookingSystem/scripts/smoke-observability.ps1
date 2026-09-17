param(
    [string]$ApiUrl = 'http://localhost:8080',
    [string]$RequestPath = '/swagger/v1/swagger.json',
    [string]$ServiceName = 'BookingSystem.Identity.API',
    [string]$GrafanaUrl = 'http://localhost:3000',
    [switch]$CheckOutage
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'deploy/compose/docker-compose.observability.yaml'
$proxyUrl = "$GrafanaUrl/api/datasources/proxy/uid"
if ($ServiceName -notmatch '^[A-Za-z0-9._-]+$') {
    throw 'Use a service name containing letters, digits, dots, underscores, or hyphens.'
}

function Get-HttpCount {
    $expression = [uri]::EscapeDataString("sum(http_server_request_duration_seconds_count{service_name=`"$ServiceName`"})")
    $result = Invoke-RestMethod "$proxyUrl/prometheus/api/v1/query?query=$expression"
    if ($result.data.result.Count -eq 0) { return 0 }
    return [double]$result.data.result[0].value[1]
}

$sources = Invoke-RestMethod "$GrafanaUrl/api/datasources"
foreach ($uid in @('loki', 'tempo', 'prometheus')) {
    if (@($sources | Where-Object uid -eq $uid).Count -ne 1) {
        throw "Missing provisioned data source: $uid"
    }
}
$tempoSource = $sources | Where-Object uid -eq 'tempo'
$lokiSource = $sources | Where-Object uid -eq 'loki'
if ($tempoSource.jsonData.tracesToLogsV2.datasourceUid -ne 'loki' -or
    $tempoSource.jsonData.tracesToMetrics.datasourceUid -ne 'prometheus' -or
    $lokiSource.jsonData.derivedFields[0].matcherRegex -ne 'trace_id' -or
    $lokiSource.jsonData.derivedFields[0].datasourceUid -ne 'tempo') {
    throw 'Signal navigation is not provisioned correctly.'
}
$dashboard = Invoke-RestMethod "$GrafanaUrl/api/dashboards/uid/bookingsystem-overview"
if (-not $dashboard.meta.provisioned) { throw 'Overview dashboard is not provisioned.' }

$traceId = [guid]::NewGuid().ToString('N')
$spanId = [guid]::NewGuid().ToString('N').Substring(0, 16)
$correlationId = [guid]::NewGuid().ToString('D')
$probe = "telemetry-private-probe-$traceId"
$separator = if ($RequestPath.Contains('?')) { '&' } else { '?' }
$requestUrl = "$($ApiUrl.TrimEnd('/'))$RequestPath${separator}token=$probe"
$headers = @{ traceparent = "00-$traceId-$spanId-01"; 'X-Correlation-Id' = $correlationId }
$response = Invoke-WebRequest $requestUrl -Headers $headers -TimeoutSec 10
if ($response.StatusCode -ne 200) { throw "Unexpected API status: $($response.StatusCode)" }

Write-Host "Request succeeded. Waiting for logs, traces, and a Prometheus scrape (trace $traceId)."
$logQuery = [uri]::EscapeDataString("{service_name=`"$ServiceName`"} | trace_id=`"$traceId`"")
$deadline = [DateTime]::UtcNow.AddSeconds(45)
$ready = $false
do {
    try {
        $logs = Invoke-RestMethod "$proxyUrl/loki/loki/api/v1/query_range?query=$logQuery&limit=100"
        $trace = Invoke-RestMethod "$proxyUrl/tempo/api/traces/$traceId"
        $after = Get-HttpCount
        $exemplarQuery = [uri]::EscapeDataString("http_server_request_duration_seconds_bucket{service_name=`"$ServiceName`"}")
        $exemplars = Invoke-RestMethod "$proxyUrl/prometheus/api/v1/query_exemplars?query=$exemplarQuery"
        $exemplarTraceIds = @($exemplars.data | ForEach-Object { $_.exemplars.labels.trace_id })
        $ready = $logs.data.result.Count -gt 0 -and $after -ge 1 -and $exemplarTraceIds -contains $traceId
    }
    catch { $ready = $false }
    if (-not $ready) { Start-Sleep -Seconds 2 }
} until ($ready -or [DateTime]::UtcNow -ge $deadline)
if (-not $ready) { throw 'Timed out waiting for all three signals.' }

$requestLogs = @($logs.data.result | ForEach-Object {
    foreach ($value in $_.values) {
        if ($value[1] -eq 'HTTP request completed') { $_ }
    }
})
if ($requestLogs.Count -ne 1) { throw "Expected one centralized request log; got $($requestLogs.Count)." }
if ($requestLogs[0].stream.CorrelationId -ne $correlationId) { throw 'Correlation ID was not preserved.' }
$span = @($trace.batches.scopeSpans.spans | Where-Object kind -eq 'SPAN_KIND_SERVER')[0]
if ($null -eq $span) { throw 'No ASP.NET Core span was exported.' }
$exportedSpanId = [Convert]::ToHexString([Convert]::FromBase64String($span.spanId)).ToLowerInvariant()
if ($requestLogs[0].stream.span_id -ne $exportedSpanId) { throw 'Log and request span IDs differ.' }
if (($trace | ConvertTo-Json -Depth 30 -Compress).Contains($probe) -or
    ($logs.data.result | ConvertTo-Json -Depth 30 -Compress).Contains($probe)) {
    throw 'Synthetic token leaked into exported telemetry.'
}
Write-Host 'PASS: one request log, matching Tempo span, HTTP metrics/exemplar, privacy filter, dashboard, and navigation configuration.'

if ($CheckOutage) {
    try {
        docker compose -f $composeFile stop otel-collector
        if ($LASTEXITCODE -ne 0) { throw 'Could not stop the Collector.' }
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $outageResponse = Invoke-WebRequest "$($ApiUrl.TrimEnd('/'))$RequestPath" -TimeoutSec 10
        $watch.Stop()
        if ($outageResponse.StatusCode -ne 200) { throw 'API failed during Collector outage.' }
        Write-Host "PASS: API returned 200 during Collector outage in $($watch.ElapsedMilliseconds) ms."
    }
    finally {
        docker compose -f $composeFile start otel-collector
        if ($LASTEXITCODE -ne 0) { throw 'Could not restart the Collector.' }
    }
}
