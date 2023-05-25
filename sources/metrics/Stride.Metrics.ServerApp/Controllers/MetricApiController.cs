using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stride.Metrics.ServerApp.Data;
using Stride.Metrics.ServerApp.Dto;
using Stride.Metrics.ServerApp.Dtos;
using Stride.Metrics.ServerApp.Dtos.Agregate;
using Stride.Metrics.ServerApp.Extensions;
using Stride.Metrics.ServerApp.Helpers;
using Stride.Metrics.ServerApp.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Stride.Metrics.ServerApp.Controllers;

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
    /// Gets the installs count since forever.
    /// </summary>
    /// <returns>The installs count.</returns>

    [HttpGet("installs-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<int>> GetInstallsCount()
    {
        int totalInstalls = await _metricDbContext.MetricInstalls.CountAsync();
        int installsLast30Days = await _metricDbContext.MetricInstalls
            .Where(a => a.Created < DateTimeHelpers.NthDaysAgo(30))
            .CountAsync();

        return new() { totalInstalls, installsLast30Days };
    }

    /// <summary>
    /// Gets the number of active users in the last 30 days. See remarks.
    /// </summary>
    /// <returns>The number of active users in the last 30 days.</returns>
    /// <remarks>An active user is a user launching at least more than 5 times the Editor.</remarks>

    [HttpGet("active-users-last-days")]//not sure where is used
    public async Task<List<int>> GetActiveUsersLastDays()
    {
        int editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var activeUsersLast30Days = await _metricDbContext.MetricEvents
            .Where(a => a.AppId == editorAppId &&
                        a.MetricEventDefinition.MetricName == "OpenApplication" &&
                        a.Timestamp > DateTimeHelpers.NthDaysAgo(30))
            .GroupBy(a => a.InstallId)
            .Where(g => g.Count() > 5)
            .CountAsync();

        var activeUsersLast60To30Days = await _metricDbContext.MetricEvents
            .Where(a => a.AppId == editorAppId &&
                        a.MetricEventDefinition.MetricName == "OpenApplication" &&
                        a.Timestamp > DateTimeHelpers.NthDaysAgo(60) &&
                        a.Timestamp <= DateTimeHelpers.NthDaysAgo(30))
            .GroupBy(a => a.InstallId)
            .Where(g => g.Count() > 5)
            .CountAsync();

        return new() { activeUsersLast30Days, activeUsersLast60To30Days };
    }

    /// <summary>
    /// Gets the number of active users in the last 30 days. See remarks.
    /// </summary>
    /// <returns>The number of active users in the last 30 days.</returns>
    /// <remarks>An active user is a user launching at least more than 5 times the Editor.</remarks>

    [HttpGet("active-users")]
    public async Task<List<ActiveUsersView>> GetActiveUsers()
    {
        return null;


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
    /// Gets the installs count per month.
    /// </summary>
    /// <returns>An installs count per month.</returns>

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
    /// Gets the installs count in the last 30 days per day.
    /// </summary>
    /// <returns>The installs count in the last 30 days per day</returns>

    [HttpGet("installs-last-days")]
    public async Task<List<AggregationPerDay>> GetInstallLastDays()
    {
        var installsLast30Days = await _metricDbContext.MetricInstalls
            .Where(a => a.Created > DateTimeHelpers.NthDaysAgo(30))
            .GroupBy(a => a.Created.Date)
            .Select(g => new AggregationPerDay { Date = g.Key, Count = g.Count() })
            .OrderBy(aggregation => aggregation.Date)
            .ToListAsync();

        return installsLast30Days;
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
                .Where(e => daysInPast != 0 ? EF.Functions.DateDiffDay(e.Timestamp, DateTime.Now) < daysInPast : true)
                .GroupBy(e => new { e.InstallId, e.IPAddress })
                .Select(g => new { Value = _metricDbContext.IPAddressToCountry(g.Key.IPAddress) });

        var countries = q
                .Where(e => e.Value != "-")
                .GroupBy(e => new { Value = e.Value })
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
    /// Gets the active users per day.
    /// </summary>
    /// <returns>The active users per day.</returns>

    [HttpGet("active-users-per-day/{minNumberOfLaunch}")]
    public async Task<List<AggregationPerDays>> GetActiveUsersPerDay(uint minNumberOfLaunch)
    {
        int editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var result = await (
    from a in _metricDbContext.MetricEvents
    join b in _metricDbContext.MetricEventDefinitions on a.MetricId equals b.MetricId
    where b.MetricName == "OpenApplication" &&
          a.AppId == editorAppId
    group a by new
    {
        Year = a.Timestamp.Year,
        Month = a.Timestamp.Month,
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

    /// <summary>
    /// Gets the usage count per version in the last 30 days.
    /// </summary>
    /// <returns>The usage count per version in the last 30 days.</returns>

    [HttpGet("usage-per-version")]
    public async Task<List<AggregationPerVersion>> GetUsagePerVersion()
    {
        int editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var usagePerVersion = await _metricDbContext.MetricEvents
            .Where(a => a.AppId == editorAppId &&
                        a.MetricEventDefinition.MetricName == "OpenApplication" &&
                        a.Timestamp >= DateTimeHelpers.NthDaysAgo(30))
            .GroupBy(a => a.MetricValue)
            .Select(g => new AggregationPerVersion
            {
                Version = g.Key,
                Count = g.Select(a => a.InstallId).Distinct().Count()
            })
            .OrderBy(usage => usage.Version)
            .ToListAsync();

        return usagePerVersion;
    }

    /// <summary>
    /// Gets the usage count per version in the last 30 days.
    /// </summary>
    /// <returns>The usage count per version in the last 30 days.</returns>

    [HttpGet("high-usage")]
    public List<AggregationPerMonth> GetHighUsage()
    {
        //var cache = await _metricDbContext.MetricCache
        //.Where(m => m.Type == "get-high-usage-job")
        //.Select(m => new CachedResult(m.JsonData))
        //.FirstOrDefaultAsync();

        //return JsonConvert.DeserializeObject<List<AggregationPerMonth>>(cache.JsonData);

        return null;
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

        return vsxDownloads;
    }

    /// <summary>
    /// Gets the crashes count per version in the last 30 days.
    /// </summary>
    /// <returns>The crashes count per version</returns>

    [HttpGet("crashes-per-version")]
    public List<CrashAggregationResult> GetCrashesPerVersion()
    {
        int editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var crashesPerVersion = _metricDbContext.MetricEvents
            .Join(
                _metricDbContext.MetricEventDefinitions,
                a => a.MetricId,
                b => b.MetricId,
                (a, b) => new { a, b }
            )
            .Join(
                _metricDbContext.MetricEvents
                    .Where(a => a.MetricId == _metricDbContext.MetricEventDefinitions
                        .Where(b => b.MetricName == "CrashedSession")
                        .Select(b => b.MetricId)
                        .FirstOrDefault()
                    )
                    .Select(a => new { a.InstallId, a.SessionId, MetricSent = a.MetricValue }),
                ab => new { ab.a.InstallId, ab.a.SessionId },
                sq => new { sq.InstallId, sq.SessionId },
                (ab, sq) => new { ab.a, ab.b, sq }
            )
            .Where(ab => ab.a.MetricId == ab.b.MetricId && ab.b.MetricName == "OpenApplication" && ab.sq.InstallId == ab.a.InstallId && ab.sq.SessionId == ab.a.SessionId && ab.a.AppId == editorAppId && (DateTime.Now - ab.a.Timestamp).Days < 30)
            .GroupBy(ab => new { ab.sq.InstallId, ab.sq.SessionId, ab.a.MetricValue, ab.sq.MetricSent })
            .OrderBy(ab => ab.Key.MetricValue)
            .Select(ab => new
            {
                ab.Key.InstallId,
                ab.Key.SessionId,
                ab.Key.MetricSent,
                Version = ab.Key.MetricValue
            })
            .ToList();

        var versionAndCrashes = new Dictionary<string, int>();
        foreach (var crashAggregation in crashesPerVersion)
        {
            if (crashAggregation.Version.StartsWith("1.20")) continue;
            if (crashAggregation.Version.Contains("alpha")) continue;
            if (versionAndCrashes.ContainsKey(crashAggregation.Version))
            {
                var current = versionAndCrashes[crashAggregation.Version];
                versionAndCrashes[crashAggregation.Version] = current + 1;
            }
            else
            {
                versionAndCrashes.Add(crashAggregation.Version, 1);
            }
        }

        var versionActivity = new Dictionary<string, decimal>();
        // foreach (var activityData in activity)
        // {
        //     versionActivity.Add(activityData.Version, activityData.Time);
        // }

        var res = new List<CrashAggregationResult>();
        foreach (var versionAndCrash in versionAndCrashes)
        {
            decimal time;
            if (versionActivity.TryGetValue(versionAndCrash.Key, out time))
            {
                var ratio = versionAndCrash.Value / ((double)time / 60.0 / 60.0);
                res.Add(new CrashAggregationResult(versionAndCrash.Key, ratio));
            }
        }

        return res;
    }

    /// <summary>
    /// Gets the platforms usage
    /// </summary>
    /// <returns>The crashes count per version in the last 30 days.</returns>

    [HttpGet("platforms-usage")]
    public List<AggregationPerPlatforms> GetPlatformsUsage()
    {
        // Get active users of version per month

        var result = _metricDbContext.MetricEvents
            .Join(_metricDbContext.MetricEventDefinitions,
                me => me.MetricId,
                med => med.MetricId,
                (me, med) => new { me, med })
            .Where(x => x.med.MetricName == "OpenSession2" && x.me.Timestamp >= DateTimeHelpers.NthDaysAgo(31))
            .Select(x => x.me.MetricValue)
            .ToList();

        var dict = new Dictionary<string, int>();
        foreach (var val in result)
        {
            var split = val.Split('|', '&');
            foreach (var v in split.Where(v => v.StartsWith("#platform:")))
            {
                var platform = v.Substring(10);
                //rename None to Package for better understanding
                if (platform == "None")
                {
                    platform = "Package";
                }

                if (dict.ContainsKey(platform))
                {
                    dict[platform] += 1;
                }
                else
                {
                    dict.Add(platform, 1);
                }
            }
        }

        var sortedValues = dict.OrderBy(x => x.Value).ToDictionary(x => x.Key, y => y.Value);

        return sortedValues.Keys.Select(key => new AggregationPerPlatforms { Platform = key, Count = dict[key] }).ToList();
    }

    /// <summary>
    /// Gets the usage count per version in the last 30 days.
    /// </summary>
    /// <returns>The usage count per version in the last 30 days.</returns>

    [HttpGet("projects-users")]
    public IEnumerable<ProjectsUsersAggregation> GetProjectsUsers()
    {
        var result = _metricDbContext.MetricEvents
            .Where(a => a.MetricId == 19)
            .Select(a => new
            {
                ProjectUUID = a.MetricValue.Substring(12, 36),
                a.InstallId,
                a.MetricValue,
                Year = a.Timestamp.Year,
                Month = a.Timestamp.Month
            })
            .Where(n => n.ProjectUUID != Guid.Empty.ToString())
            .GroupBy(n => new { n.Month, n.Year, n.ProjectUUID, n.MetricValue })
            .Where(g => g.Count() > 1)
            .Select(g => new
            {
                Count = g.Count(),
                g.Key.ProjectUUID,
                g.Key.MetricValue,
                g.Key.Month,
                g.Key.Year
            })
            .GroupBy(m => new { m.Month, m.Year })
            .Select(g => new ProjectsUsersAggregation
            (
                g.Key.Year,
                g.Key.Month,
                g.Sum(x => x.Count > 1 ? 1 : 0),
                g.Sum(x => x.Count > 3 ? 1 : 0),
                g.Sum(x => x.Count > 5 ? 1 : 0)
            ))
            .ToList();
        return result;
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
        MetricEvent result = _metricDbContext.SaveNewMetric(newMetric, ipAddress);

        _logger.LogInformation("/api/push-metric New metric saved in {0}ms: {1}", clock.ElapsedMilliseconds, JsonConvert.SerializeObject(result));

        return Ok();
    }
}
