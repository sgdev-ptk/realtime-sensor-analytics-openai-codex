using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorAnalytics.Api.Hubs;
using SensorAnalytics.Api.Models;

namespace SensorAnalytics.Api.Services;

public class SensorSimulationService : BackgroundService
{
    private const int SensorsPerBatch = 10;
    public const int ReadingsPerSecond = 1_000;
    private readonly ILogger<SensorSimulationService> _logger;
    private readonly SensorDataStore _dataStore;
    private readonly IHubContext<SensorHub> _hubContext;
    private readonly Random _random = new();
    private readonly string[] _sensorIds = Enumerable.Range(1, SensorsPerBatch)
        .Select(i => $"sensor-{i:000}").ToArray();

    public SensorSimulationService(
        ILogger<SensorSimulationService> logger,
        SensorDataStore dataStore,
        IHubContext<SensorHub> hubContext)
    {
        _logger = logger;
        _dataStore = dataStore;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting sensor simulation service");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                var readings = GenerateBatch(timestamp).ToList();
                var alerts = new List<AlertEvent>();

                foreach (var reading in readings)
                {
                    _dataStore.AddReading(reading);
                    if (IsAnomaly(reading))
                    {
                        var alert = new AlertEvent(
                            reading.Id,
                            reading.SensorId,
                            reading.Timestamp,
                            reading.Value,
                            $"Anomalous value detected: {reading.Value:F2}",
                            "critical");
                        alerts.Add(alert);
                        _dataStore.AddAlert(alert);
                    }
                }

                var aggregate = _dataStore.GetAggregate();

                await _hubContext.Clients.All.SendAsync(
                    SensorHub.StreamEvent,
                    new
                    {
                        readings,
                        aggregate,
                        alerts
                    },
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // normal during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating sensor readings");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private IEnumerable<SensorReading> GenerateBatch(DateTime timestamp)
    {
        var interval = 1.0 / ReadingsPerSecond;
        for (var i = 0; i < ReadingsPerSecond; i++)
        {
            var readingTimestamp = timestamp.AddSeconds(i * interval);
            var sensorId = _sensorIds[i % _sensorIds.Length];
            var baseValue = 50 + 10 * Math.Sin((timestamp - DateTime.UnixEpoch).TotalSeconds / 30.0);
            var value = baseValue + _random.NextDouble() * 10 - 5;
            if (_random.NextDouble() < 0.01)
            {
                value += _random.NextDouble() * 80 - 40;
            }

            var temperature = 20 + _random.NextDouble() * 5;
            var humidity = 50 + _random.NextDouble() * 10;

            yield return new SensorReading(
                Guid.NewGuid(),
                sensorId,
                readingTimestamp,
                value,
                temperature,
                humidity);
        }
    }

    private static bool IsAnomaly(SensorReading reading)
    {
        return reading.Value is < 15 or > 85;
    }
}
