using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Stride.Metrics.ServerApp.Data;
using Stride.Metrics.ServerApp.Extensions;

namespace Stride.Metrics.ServerApp.Controllers.Api;

[ApiController()]
[Route("api")]
public class MetricApiController : ControllerBase
{

    /// <summary>
    /// Use this guid on the client side in the variable StrideMetricsSpecial to avoid loggin usage
    /// </summary>
    private static readonly Guid ByPassSpecialGuid = new("AEA51F92-84DD-40D7-BFE6-442F37A308D6");

    private readonly MetricDbContext _metricDbContext;
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MetricApiController(
        MetricDbContext metricDbContext,
        ILogger<HomeController> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _metricDbContext = metricDbContext;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Pushes the specified new metric.
    /// </summary>
    /// <param name="newMetric">The new metric.</param>
    /// <returns>Ok() unless an error occurs</returns>
    [HttpPost("push-metric")]
    public IActionResult Push(NewMetricMessage newMetric)
    {
        // If special guid, then don't store anything
        var ipAddress = _httpContextAccessor.GetIPAddress();

        if (newMetric.SpecialId == ByPassSpecialGuid) //filter out SSKK by default
        {
            Trace.TraceInformation("/api/push-metric (origin: {0}) with SpecialGuid not saved {1}", ipAddress, JsonConvert.SerializeObject(newMetric));
            return Ok();
        }

        var clock = Stopwatch.StartNew();
        var result = _metricDbContext.SaveNewMetric(newMetric, ipAddress);

        _logger.LogInformation("/api/push-metric New metric saved in {0}ms: {1}", clock.ElapsedMilliseconds, JsonConvert.SerializeObject(result));

        return Ok();
    }
}
