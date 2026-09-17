namespace BookingSystem.ApiGateway.Middleware;

public sealed class GatewayRequestState
{
    public required CancellationToken ClientDisconnected { get; init; }
    public bool UpstreamResponseReceived { get; set; }
    public bool ErrorWritten { get; set; }
    public bool GatewayAborted { get; set; }
    public Uri? UpstreamOrigin { get; set; }
    public string ErrorCode { get; set; } = "None";
    public string ForwarderErrorCode { get; set; } = "None";
}
