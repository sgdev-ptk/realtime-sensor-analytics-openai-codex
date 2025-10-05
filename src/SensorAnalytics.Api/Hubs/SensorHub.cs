using Microsoft.AspNetCore.SignalR;

namespace SensorAnalytics.Api.Hubs;

public class SensorHub : Hub
{
    public const string StreamEvent = "ReceiveSensorBatch";
}
