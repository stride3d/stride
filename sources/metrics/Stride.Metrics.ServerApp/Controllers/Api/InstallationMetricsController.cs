using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stride.Metrics.ServerApp.Data;
using Stride.Metrics.ServerApp.Dto;
using Stride.Metrics.ServerApp.Dtos;
using Stride.Metrics.ServerApp.Dtos.Agregate;
using Stride.Metrics.ServerApp.Helpers;

namespace Stride.Metrics.ServerApp.Controllers.Api;

public class InstallationMetricsController
{
    private readonly MetricDbContext _metricDbContext;

    public InstallationMetricsController(MetricDbContext metricDbContext)
    {
        _metricDbContext = metricDbContext;
    }

    /// <summary>
    /// Gets the total installs
    /// </summary>
    /// <returns>An total installs.</returns>

    [HttpGet("installs-trend")]
    public async Task<List<YearProjection>> GetInstallsTrend()
    {
        var installsPerMonth = await _metricDbContext.MetricInstalls
        .GroupBy(mi => new { mi.Created.Year })
        .Select(g => new YearProjection
        {
            Year = g.Key.Year,
            January = g.Count(mi => mi.Created.Month == 1),
            February = g.Count(mi => mi.Created.Month == 2),
            March = g.Count(mi => mi.Created.Month == 3),
            April = g.Count(mi => mi.Created.Month == 4),
            May = g.Count(mi => mi.Created.Month == 5),
            June = g.Count(mi => mi.Created.Month == 6),
            July = g.Count(mi => mi.Created.Month == 7),
            August = g.Count(mi => mi.Created.Month == 8),
            September = g.Count(mi => mi.Created.Month == 9),
            October = g.Count(mi => mi.Created.Month == 10),
            November = g.Count(mi => mi.Created.Month == 11),
            December = g.Count(mi => mi.Created.Month == 12),
            Count = g.Count()
        })
        .OrderBy(y => y.Year)
        .ToListAsync();

        var pastCount = 0;
        foreach (var yearProjection in installsPerMonth)
        {
            yearProjection.January += pastCount;
            yearProjection.February += yearProjection.January;
            yearProjection.March += yearProjection.February;
            yearProjection.April += yearProjection.March;
            yearProjection.May += yearProjection.April;
            yearProjection.June += yearProjection.May;
            yearProjection.July += yearProjection.June;
            yearProjection.August += yearProjection.July;
            yearProjection.September += yearProjection.August;
            yearProjection.October += yearProjection.September;
            yearProjection.November += yearProjection.October;
            yearProjection.December += yearProjection.November;
            pastCount += yearProjection.Count;
        }

        return installsPerMonth;

    }

    /// <summary>
    /// Gets the installs count per month.
    /// </summary>
    /// <returns>An installs count per month.</returns>
    [HttpGet("installs-per-month")]
    public async Task<List<AggregationPerMonth>> GetInstallsPerMonth()
    {
        // Get installations per month
        var installsPerMonth = await _metricDbContext.MetricInstalls
            .GroupBy(a => new
            {
                a.Created.Year,
                a.Created.Month
            })
            .Select(g => new AggregationPerMonth { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
            .OrderBy(a => a.Year)
                .ThenBy(a => a.Month)
            .ToListAsync();

        return installsPerMonth;
    }

    /// <summary>
    /// Gets the installs count in the last 30 days per day.
    /// </summary>
    /// <returns>The installs count in the last 30 days per day</returns>
    [HttpGet]
    [Route("installs-last-days")]
    public List<AggregationPerDay> GetInstallLastDays()
    {
        // Get installations per day in the last 30 days
        var installsLast30Days = _metricDbContext.MetricInstalls
                    .Where(mi => mi.Created > DateTimeHelpers.NthDaysAgo(30))
                    .GroupBy(mi => mi.Created)
                    .OrderBy(mi => mi.Key)
                    .Select(mi => new AggregationPerDay() { Count = mi.Count(), Date = mi.Key })
                    .ToList();

        return installsLast30Days;
    }

    /// <summary>
    /// Gets the users quits per month.
    /// </summary>
    /// <returns>The users quits per month.</returns>

    [HttpGet("quitting-count")]
    public IEnumerable<AggregationPerMonth> GetQuittingCount()
    {
        int editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var installs = (
            from a in _metricDbContext.MetricEvents
            join b in _metricDbContext.MetricEventDefinitions on a.MetricId equals b.MetricId
            where b.MetricName == "OpenApplication" &&
                  a.AppId == editorAppId &&
                  EF.Functions.DateDiffDay(a.Timestamp, DateTime.Now) < 31
            group a by new
            {
                Year = a.Timestamp.Year,
                Month = a.Timestamp.Month,
                a.InstallId
            } into g
            select new AggregationPerInstall
            (
                g.Key.Year,
                g.Key.Month,
                g.Key.InstallId
            ))
            .ToList();

        var result = new List<AggregationPerMonth>();
        var installsCopy = installs.ToList();
        foreach (var aggregationPerInstall in installs)
        {
            var date = new DateTime(aggregationPerInstall.Year, aggregationPerInstall.Month, 1);
            if (result.Count == 0)
            {
                result.Add(new AggregationPerMonth
                {
                    Count = 0,
                    Year = aggregationPerInstall.Year,
                    Month = aggregationPerInstall.Month
                });
            }

            var future = date.AddMonths(1);
            if (future > new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1))
            {
                continue;
            }

            var persists = installsCopy.Any(
                x => aggregationPerInstall.InstallId == x.InstallId && new DateTime(x.Year, x.Month, 1) == future);
            if (persists) continue;
            {
                var resultMonth = result.FirstOrDefault(x => x.Year == future.Year && x.Month == future.Month);
                if (resultMonth == null)
                {
                    resultMonth = new AggregationPerMonth
                    {
                        Month = future.Month,
                        Year = future.Year
                    };
                    result.Add(resultMonth);
                }

                resultMonth.Count++;
            }
        }
        return result;

    }
    /// <summary>
    /// Gets the number of Stride downloads
    /// </summary>
    /// <returns>The number of Stride downloads per month.</returns>

    [HttpGet("stride-downloads")]
    public IEnumerable<AggregationPerMonth> GetStrideDownloads()
    {
        var strideDownloads = (
            from a in _metricDbContext.MetricEvents
            join b in _metricDbContext.MetricEventDefinitions on a.MetricId equals b.MetricId
            where b.MetricName == "DownloadPackage" &&
                a.MetricValue.Contains("Stride") &&
                a.MetricValue.Contains("DownloadCompleted")
            group a by new
            {
                Year = a.Timestamp.Year,
                Month = a.Timestamp.Month,
                a.InstallId
            } into g
            select new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.InstallId
            })
            .AsEnumerable()
            .GroupBy(g => new
            {
                g.Year,
                g.Month
            })
            .Select(g => new AggregationPerMonth
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(r => r.Year)
            .ThenBy(r => r.Month)
            .ToList();

        return strideDownloads;

    }

    /// <summary>
    /// Gets the number of downloads of Stride3D's Visual Studio plugin
    /// </summary>
    /// <returns>The number of Visual Studio plugin downloads per month.</returns>

    [HttpGet("vsx-downloads")]
    public IEnumerable<AggregationPerMonth> GetVsxDownloads()
    {
        int editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var vsxDownloads = (
            from a in _metricDbContext.MetricEvents
            join b in _metricDbContext.MetricEventDefinitions on a.MetricId equals b.MetricId
            where b.MetricName == "DownloadPackage" &&
                a.MetricValue.Contains("VisualStudio") &&
                a.MetricValue.Contains("DownloadCompleted")
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
            .AsEnumerable()
            .GroupBy(g => new
            {
                g.Year,
                g.Month
            })
            .Select(g => new AggregationPerMonth
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(r => r.Year)
            .ThenBy(r => r.Month)
            .ToList();

        return vsxDownloads;
    }
}
