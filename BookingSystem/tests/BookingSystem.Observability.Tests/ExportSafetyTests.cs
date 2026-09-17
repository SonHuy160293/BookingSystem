using System.Collections.Concurrent;
using System.Diagnostics;
using BookingSystem.AspNetCore.DependencyInjection;
using BookingSystem.AspNetCore.Middleware;
using BookingSystem.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Serilog;
using Xunit;

namespace BookingSystem.Observability.Tests;

public sealed class ExportSafetyTests
{
    [Fact]
    public async Task RequestSucceedsDuringCollectorOutageAndExportsOneSafeCorrelatedLogAsync()
    {
        using var logs = new CapturingLogExporter();
        using var traces = new CapturingTraceExporter();
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:1",
            ["Observability:ExportTimeoutMilliseconds"] = "500"
        });
        builder.Logging.ClearProviders();
        builder.AddBookingSystemObservability();
        builder.AddBookingSystemObservability();
        builder.AddConfiguredSerilog();
        builder.Logging.AddOpenTelemetry(logging => logging.AddProcessor(new SimpleLogRecordExportProcessor(logs)));
        builder.Services.ConfigureOpenTelemetryTracerProvider((services, tracing) =>
            tracing.AddProcessor(new SimpleActivityExportProcessor(traces)));
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        await using var app = builder.Build();
        app.UseCorrelationId();
        app.UseSerilogRequestLogging();
        app.MapGet("/test/{id}", (ILogger<ExportSafetyTests> logger) =>
        {
            logger.LogInformation(
                new InvalidOperationException("private-exception"),
                "Private {Password} for {Email}",
                "private-password",
                "private-email");
            return "ok";
        });
        await app.StartAsync();
        string address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient();
        string traceId = ActivityTraceId.CreateRandom().ToHexString();
        using var caller = new Activity("Test caller").SetParentId(
            $"00-{traceId}-{ActivitySpanId.CreateRandom().ToHexString()}-01").Start();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{address}/test/private-path?token=private-token");
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom().ToHexString()}-01");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());

        caller.Stop();

        // Give the bounded exporter a chance to attempt a failed export, then request again.
        await Task.Delay(1500);
        (await client.GetAsync($"{address}/test/another")).EnsureSuccessStatusCode();
        await app.StopAsync();

        var record = Assert.Single(logs.Records, record =>
            record.Category == typeof(ExportSafetyTests).FullName && record.TraceId == traceId);
        Assert.Equal("Application log", record.Body);
        Assert.DoesNotContain("private-", record.Attributes, StringComparison.Ordinal);
        Assert.Null(record.Exception);
        var span = Assert.Single(traces.Spans, span => span.TraceId == traceId && span.Kind == ActivityKind.Server);
        Assert.Equal("/test/{id}", span.Tags.Single(tag => tag.Key == "http.route").Value);
        Assert.DoesNotContain(span.Tags, tag => tag.Value?.ToString()?.Contains("private-", StringComparison.Ordinal) == true);
        Assert.Equal("HTTP request", span.DisplayName);
    }

    private sealed class CapturingLogExporter : BaseExporter<LogRecord>
    {
        internal ConcurrentQueue<LogSnapshot> Records { get; } = new();

        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            foreach (var record in batch)
            {
                Records.Enqueue(new(record.CategoryName, record.TraceId.ToHexString(), record.Body,
                    string.Join(";", record.Attributes ?? []), record.Exception));
            }

            return ExportResult.Success;
        }
    }

    private sealed class CapturingTraceExporter : BaseExporter<Activity>
    {
        internal ConcurrentQueue<TraceSnapshot> Spans { get; } = new();

        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch)
            {
                Spans.Enqueue(new(activity.TraceId.ToHexString(), activity.Kind, activity.DisplayName, activity.TagObjects.ToArray()));
            }

            return ExportResult.Success;
        }
    }

    private sealed class LogSnapshot
    {
        internal LogSnapshot(string? category, string traceId, string? body, string attributes, Exception? exception)
        {
            Category = category;
            TraceId = traceId;
            Body = body;
            Attributes = attributes;
            Exception = exception;
        }

        internal string? Category { get; }

        internal string TraceId { get; }

        internal string? Body { get; }

        internal string Attributes { get; }

        internal Exception? Exception { get; }
    }

    private sealed class TraceSnapshot
    {
        internal TraceSnapshot(string traceId, ActivityKind kind, string displayName, KeyValuePair<string, object?>[] tags)
        {
            TraceId = traceId;
            Kind = kind;
            DisplayName = displayName;
            Tags = tags;
        }

        internal string TraceId { get; }

        internal ActivityKind Kind { get; }

        internal string DisplayName { get; }

        internal KeyValuePair<string, object?>[] Tags { get; }
    }
}
