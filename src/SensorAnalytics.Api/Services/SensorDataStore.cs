using System.Linq;
using SensorAnalytics.Api.Models;

namespace SensorAnalytics.Api.Services;

public class SensorDataStore
{
    private readonly Queue<SensorReading> _readings = new();
    private readonly LinkedList<AlertEvent> _alerts = new();
    private readonly object _lock = new();

    private readonly TimeSpan _retention;
    private readonly int _maxDataPoints;
    private readonly int _maxAlertHistory;

    private double _sumValue;
    private double _sumTemperature;
    private double _sumHumidity;
    private double _sumValueSquared;
    private double _minValue = double.MaxValue;
    private double _maxValue = double.MinValue;

    public SensorDataStore(TimeSpan? retention = null, int maxDataPoints = 100_000, int maxAlertHistory = 200)
    {
        _retention = retention ?? TimeSpan.FromHours(24);
        _maxDataPoints = maxDataPoints;
        _maxAlertHistory = maxAlertHistory;
    }

    public void AddReading(SensorReading reading)
    {
        lock (_lock)
        {
            PurgeExpired(reading.Timestamp);

            if (_readings.Count >= _maxDataPoints)
            {
                RemoveOldest();
            }

            _readings.Enqueue(reading);
            _sumValue += reading.Value;
            _sumTemperature += reading.Temperature;
            _sumHumidity += reading.Humidity;
            _sumValueSquared += reading.Value * reading.Value;
            if (reading.Value < _minValue)
            {
                _minValue = reading.Value;
            }

            if (reading.Value > _maxValue)
            {
                _maxValue = reading.Value;
            }
        }
    }

    public void AddAlert(AlertEvent alert)
    {
        lock (_lock)
        {
            _alerts.AddFirst(alert);
            while (_alerts.Count > _maxAlertHistory)
            {
                _alerts.RemoveLast();
            }
        }
    }

    public IReadOnlyList<SensorReading> GetRecentReadings(int count)
    {
        lock (_lock)
        {
            return _readings.TakeLast(Math.Min(count, _readings.Count)).ToArray();
        }
    }

    public IReadOnlyList<AlertEvent> GetRecentAlerts(int count)
    {
        lock (_lock)
        {
            return _alerts.Take(count).ToArray();
        }
    }

    public SensorAggregate GetAggregate()
    {
        lock (_lock)
        {
            if (_readings.Count == 0)
            {
                var now = DateTime.UtcNow;
                return new SensorAggregate(0, 0, 0, 0, 0, 0, 0, now, now);
            }

            var windowStart = _readings.Peek().Timestamp;
            var windowEnd = _readings.Last().Timestamp;
            var count = _readings.Count;
            var average = _sumValue / count;
            var avgTemp = _sumTemperature / count;
            var avgHumidity = _sumHumidity / count;
            var variance = (_sumValueSquared / count) - (average * average);
            var stdDev = Math.Sqrt(Math.Max(variance, 0));

            return new SensorAggregate(
                count,
                average,
                _minValue,
                _maxValue,
                avgTemp,
                avgHumidity,
                stdDev,
                windowStart,
                windowEnd);
        }
    }

    private void PurgeExpired(DateTime currentTimestamp)
    {
        while (_readings.Count > 0 && currentTimestamp - _readings.Peek().Timestamp > _retention)
        {
            RemoveOldest();
        }
    }

    private void RemoveOldest()
    {
        var removed = _readings.Dequeue();
        _sumValue -= removed.Value;
        _sumTemperature -= removed.Temperature;
        _sumHumidity -= removed.Humidity;
        _sumValueSquared -= removed.Value * removed.Value;

        if (Math.Abs(removed.Value - _minValue) < double.Epsilon || Math.Abs(removed.Value - _maxValue) < double.Epsilon)
        {
            if (_readings.Count == 0)
            {
                _minValue = double.MaxValue;
                _maxValue = double.MinValue;
                return;
            }

            _minValue = _readings.Min(r => r.Value);
            _maxValue = _readings.Max(r => r.Value);
        }
    }
}
