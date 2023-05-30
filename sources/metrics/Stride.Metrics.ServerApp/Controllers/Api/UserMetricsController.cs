using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stride.Metrics.ServerApp.Data;
using Stride.Metrics.ServerApp.Dtos.Agregate;

namespace Stride.Metrics.ServerApp.Controllers.Api;

public class UserMetricsController
{
    private readonly MetricDbContext _metricDbContext;

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
                .Where(e => (daysInPast == 0) || EF.Functions.DateDiffDay(e.Timestamp, DateTime.Now) < daysInPast)
                .GroupBy(e => new { e.InstallId, e.IPAddress })
                .Select(g => new { Value = _metricDbContext.IPAddressToCountry(g.Key.IPAddress) });

        var countries = q
                .Where(e => e.Value != "-")
                .GroupBy(e => new { e.Value })
                .Where(g => g.Count() > 5)
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

        var query = (
            from a in _metricDbContext.MetricEvents
            join b in _metricDbContext.MetricEventDefinitions on a.MetricId equals b.MetricId
            where b.MetricName == "OpenApplication" &&
                  a.AppId == editorAppId &&
                  EF.Functions.DateDiffDay(a.Timestamp, DateTime.Now) < 31
            group a by new
            {
                a.Timestamp.Year,
                a.Timestamp.Month,
                a.InstallId
            } into g
            select new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.InstallId
            })
            .ToList();

        var result = query
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

        return result;
    }
    /// <summary>
    /// Gets the active users per day.
    /// </summary>
    /// <returns>The active users per day.</returns>

    [HttpGet("active-users-per-day/{minNumberOfLaunch}")]
    public async Task<List<AggregationPerDays>> GetActiveUsersPerDay(uint minNumberOfLaunch)
    {
        var editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var result = await (
        from a in _metricDbContext.MetricEvents
        join b in _metricDbContext.MetricEventDefinitions on a.MetricId equals b.MetricId
        where b.MetricName == "OpenApplication" &&
              a.AppId == editorAppId
        group a by new
        {
            a.Timestamp.Year,
            a.Timestamp.Month,
            a.InstallId
        } into g
        where g.Count() > minNumberOfLaunch
        orderby g.Key.Year, g.Key.Month
        select new
        {
            g.Key.Year,
            g.Key.Month,
            Count = g.Count()
        })
        .ToListAsync();

        var finalResult = result.Select(r => new AggregationPerDays
        {
            Year = r.Year,
            Month = r.Month,
            Count = r.Count
        }).ToList();

        return finalResult;
    }

}
