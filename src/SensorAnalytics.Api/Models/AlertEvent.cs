namespace SensorAnalytics.Api.Models;

public record AlertEvent(
    Guid Id,
    string SensorId,
    DateTime Timestamp,
    double Value,
    string Message,
    string Severity);
