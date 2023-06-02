using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stride.Metrics.ServerApp.Data;
using Stride.Metrics.ServerApp.Dtos.Agregate;
using Stride.Metrics.ServerApp.Extensions;

namespace Stride.Metrics.ServerApp.Controllers.Api;

///<summary>Includes user related actions</summary>
[ApiController()]
[Route("api")]
public class UserMetricsController
{
    private readonly MetricDbContext _metricDbContext;
    ///<summary>Includes user related actions</summary>
    public UserMetricsController(MetricDbContext metricDbContext)
    {
        _metricDbContext = metricDbContext;
    }

    /// <summary>
    /// Gets the number of users per Country
    /// </summary>
    /// <returns>The number of users from the top 20 countries</returns>

    [HttpGet("countries/{daysInPast}")]
    public IEnumerable<AggregationPerValue> GetCountries(int daysInPast)
    {
        int editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var q = _metricDbContext.MetricEvents
                .Include(e => e.MetricEventDefinition)
                .Where(e => e.MetricEventDefinition.MetricName == "OpenApplication")
                .Where(e => e.AppId == editorAppId)
                .Where(e => daysInPast == 0 || EF.Functions.DateDiffDay(e.Timestamp, DateTime.Now) < daysInPast)
                .GroupBy(e => new { e.InstallId, e.IPAddress })
                .Select(g => new { Value = _metricDbContext.IPAddressToCountry(g.Key.IPAddress) });

        var countries = q
                .GroupBy(e => new { e.Value })
                .Where(g => g.Count() > 5)
                .Where(g => g.Key.Value != "-")
                .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key.Value)
                .Take(20)
                .Select(g => new AggregationPerValue
                {
                    Count = g.Count(),
                    Value = g.Key.Value
                });

        return countries;
    }

    /// <summary>
    /// Gets the active users per month.
    /// </summary>
    /// <returns>The active users per month.</returns>

    [HttpGet("active-users-per-month/{minNumberOfLaunch}")]
    public List<AggregationPerMonth> GetActiveUsersPerMonth(int minNumberOfLaunch)
    {
        int editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var activeUsers = _metricDbContext.MetricEvents
            .Where(ev => ev.MetricEventDefinition.MetricName == "OpenApplication"
                && ev.AppId == editorAppId)
            .GroupBy(ev => new
            {
                ev.Timestamp.Year,
                ev.Timestamp.Month,
                ev.InstallId
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.InstallId
            })
            .GroupBy(x => new { x.Year, x.Month })
            .Where(g => g.Count() > minNumberOfLaunch)
            .Select(g => new AggregationPerMonth
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
            .ToList();

        return activeUsers;
    }
    /// <summary>
    /// Gets the active users per day.
    /// </summary>
    /// <returns>The active users per day.</returns>

    [HttpGet("active-users-per-day/{minNumberOfLaunch}")]
    public async Task<List<AggregationPerDay>> GetActiveUsersPerDay(uint minNumberOfLaunch)
    {
        var editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var activeUsers = await _metricDbContext.MetricEvents
           .Where(ev => ev.MetricEventDefinition.MetricName == "OpenApplication"
               && ev.AppId == editorAppId)
           .GroupBy(ev => new
           {
               ev.Timestamp.Year,
               ev.Timestamp.Month,
               ev.Timestamp.Day,
               ev.InstallId
           })
           .Select(g => new
           {
               g.Key.Year,
               g.Key.Month,
               g.Key.Day,
               g.Key.InstallId
           })
           .GroupBy(x => new { x.Year, x.Month, x.Day })
           .Where(g => g.Count() > minNumberOfLaunch)
           .Select(g => new AggregationPerDay
           {
               Year = g.Key.Year,
               Month = g.Key.Month,
               Day = g.Key.Day,
               Count = g.Count()
           })
           .OrderBy(r => r.Year)
               .ThenBy(r => r.Month)
           .ToListAsync();

        return activeUsers;
    }

}
