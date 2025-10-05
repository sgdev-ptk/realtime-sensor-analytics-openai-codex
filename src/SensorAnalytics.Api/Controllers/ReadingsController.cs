using Microsoft.AspNetCore.Mvc;
using SensorAnalytics.Api.Models;
using SensorAnalytics.Api.Services;

namespace SensorAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly SensorDataStore _dataStore;

    public ReadingsController(SensorDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpGet("stats")]
    public ActionResult<SensorAggregate> GetStats()
    {
        return Ok(_dataStore.GetAggregate());
    }

    [HttpGet("recent")]
    public ActionResult<IEnumerable<SensorReading>> GetRecent([FromQuery] int count = 100)
    {
        return Ok(_dataStore.GetRecentReadings(count));
    }

    [HttpGet("alerts")]
    public ActionResult<IEnumerable<AlertEvent>> GetAlerts([FromQuery] int count = 50)
    {
        return Ok(_dataStore.GetRecentAlerts(count));
    }
}
