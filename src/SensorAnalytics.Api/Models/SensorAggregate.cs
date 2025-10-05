namespace SensorAnalytics.Api.Models;

public record SensorAggregate(
    int TotalReadings,
    double AverageValue,
    double MinValue,
    double MaxValue,
    double AverageTemperature,
    double AverageHumidity,
    double StandardDeviation,
    DateTime WindowStart,
    DateTime WindowEnd);
