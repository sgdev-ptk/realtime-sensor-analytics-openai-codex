using System.Text.Json.Serialization;

namespace SensorAnalytics.Api.Models;

public record SensorReading(
    Guid Id,
    string SensorId,
    DateTime Timestamp,
    double Value,
    double Temperature,
    double Humidity)
{
}
