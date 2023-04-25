using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stride.Metrics.ServerApp.Data;
using Stride.Metrics.ServerApp.Dto;
using Stride.Metrics.ServerApp.Dtos;
using Stride.Metrics.ServerApp.Dtos.Agregate;
using Stride.Metrics.ServerApp.Models;

namespace Stride.Metrics.ServerApp.Controllers;

[ApiController()]
[Route("api")]
public partial class MetricApiController : ControllerBase
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
    public async Task<List<int>> GetInstallsCount()
    {
        int totalInstalls = await _metricDbContext.Installs.CountAsync();
        int installsLast30Days = await _metricDbContext.Installs
            .Where(a => a.Created < DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        return new List<int> { totalInstalls, installsLast30Days };
    }

    /// <summary>
    /// Gets the number of active users in the last 30 days. See remarks.
    /// </summary>
    /// <returns>The number of active users in the last 30 days.</returns>
    /// <remarks>An active user is a user launching at least more than 5 times the Editor.</remarks>

    [HttpGet("active-users-last-days")]
    public async Task<List<int>> GetActiveUsersLastDays()
    {
        DateTime daysAgo30 = DateTime.UtcNow.AddDays(-30);
        DateTime daysAgo60 = DateTime.UtcNow.AddDays(-60);

        int editorAppId = MetricDbContext.AppEditorId;
        string metricName = "OpenApplication";

        var activeUsersLast30Days = await _metricDbContext.Metrics
            .Where(a => a.AppId == editorAppId &&
                        a.MetricEventDefinition.MetricName == metricName &&
                        a.Timestamp > daysAgo30)
            .GroupBy(a => a.InstallId)
            .Where(g => g.Count() > 5)
            .CountAsync();

        var activeUsersLast60To30Days = await _metricDbContext.Metrics
            .Where(a => a.AppId == editorAppId &&
                        a.MetricEventDefinition.MetricName == metricName &&
                        a.Timestamp > daysAgo60 &&
                        a.Timestamp <= daysAgo30)
            .GroupBy(a => a.InstallId)
            .Where(g => g.Count() > 5)
            .CountAsync();

        return new List<int> { activeUsersLast30Days, activeUsersLast60To30Days };
    }

    /// <summary>
    /// Gets the number of active users in the last 30 days. See remarks.
    /// </summary>
    /// <returns>The number of active users in the last 30 days.</returns>
    /// <remarks>An active user is a user launching at least more than 5 times the Editor.</remarks>

    [HttpGet("active-users")]
    public async Task<List<ActiveUsersView>> GetActiveUsers()
    {
        var cache = await _metricDbContext.MetricCache
        .Where(m => m.Type == "get-active-users-job")
        .Select(m => new CachedResult(m.JsonData))
        .FirstOrDefaultAsync();

        return JsonConvert.DeserializeObject<List<ActiveUsersView>>(cache.JsonData);
    }

    /// <summary>
    /// Gets the number of active users in the last 30 days. See remarks.
    /// </summary>
    /// <returns>The number of active users in the last 30 days.</returns>
    /// <remarks>An active user is a user launching at least more than 5 times the Editor.</remarks>

    [HttpGet("active-users-job")]
    public async Task<string> GetActiveUsersJob()
    {
        var result = await _metricDbContext.SqlToList<ActiveUsersView>(SQLRawQueries.getActiveUsers);

        var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(result) + "' WHERE Type = 'get-active-users-job'";

        await _metricDbContext.Database.ExecuteSqlRawAsync(cacheQuery);

        return "Done";
    }

    /// <summary>
    /// Gets the installs count per month.
    /// </summary>
    /// <returns>An installs count per month.</returns>
    [HttpGet("installs-per-month")]
    public async Task<List<AggregationPerMonth>> GetInstallsPerMonth()
    {
        // Get installations per month
        var installsPerMonth = await _metricDbContext.Installs
            .GroupBy(a => new
            {
                a.Created.Year,
                a.Created.Month
            })
            .Select(g => new AggregationPerMonth
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
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
        var installsPerMonth = await _metricDbContext.Installs
        .GroupBy(mi => new { Year = mi.Created.Year })
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

        return installsPerMonth;

    }

    /// <summary>
    /// Gets the installs count in the last 30 days per day.
    /// </summary>
    /// <returns>The installs count in the last 30 days per day</returns>

    [HttpGet("installs-last-days")]
    public async Task<List<AggregationPerDay>> GetInstallLastDays()
    {
        DateTime date30DaysAgo = DateTime.UtcNow.AddDays(-30);

        var installsLast30Days = await _metricDbContext.Installs
            .Where(a => a.Created > date30DaysAgo)
            .GroupBy(a => a.Created.Date)
            .Select(g => new AggregationPerDay
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(aggregation => aggregation.Date)
            .ToListAsync();

        return installsLast30Days;
    }

    /// <summary>
    /// Gets the active users per month.
    /// </summary>
    /// <returns>The active users per month.</returns>

    [HttpGet("active-users-per-month/{minNumberOfLaunch}")]
    public async Task<List<AggregationPerMonth>> GetActiveUsersPerMonth(int minNumberOfLaunch)
    {
        int editorAppId = MetricDbContext.AppEditorId;
        string metricName = "OpenApplication";

        var activeUsersPerMonth = await _metricDbContext.Metrics
            .Where(a => a.AppId == editorAppId &&
                        a.MetricEventDefinition.MetricName == metricName)
            .GroupBy(a => new
            {
                a.Timestamp.Year,
                a.Timestamp.Month,
                a.InstallId
            })
            .Where(g => g.Count() > minNumberOfLaunch)
            .GroupBy(g => new { g.Key.Year, g.Key.Month })
            .Select(g => new AggregationPerMonth
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(aggregation => aggregation.Year)
            .ThenBy(aggregation => aggregation.Month)
            .ToListAsync();

        return activeUsersPerMonth;
    }

    /// <summary>
    /// Gets the active users per month.
    /// </summary>
    /// <returns>The active users per month.</returns>

    [HttpGet("quitting-count-job")]
    public async Task<string> GetQuittingCountJob()
    {
        var installations = await _metricDbContext.SqlToList<AggregationPerInstall>(SQLRawQueries.getActiveUsersPerMonth, MetricDbContext.AppEditorId);
        var installationsCopy = installations.ToList();
        var result = new List<AggregationPerMonth>();
        foreach (var aggregationPerInstall in installations)
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

            var persists = installationsCopy.Any(
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

        var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(result) + $"' WHERE Type = 'get-quitting-count-job'";

        await _metricDbContext.Database.ExecuteSqlRawAsync(cacheQuery);

        return "Done";
    }

    /// <summary>
    /// Gets the active users per month.
    /// </summary>
    /// <returns>The active users per month.</returns>

    [HttpGet("quitting-count")]
    public async Task<List<AggregationPerMonth>> GetQuittingCount()
    {
        var cache = await _metricDbContext.MetricCache
        .Where(m => m.Type == "get-quitting-count-job")
        .Select(m => new CachedResult(m.JsonData))
        .FirstOrDefaultAsync();

        return JsonConvert.DeserializeObject<List<AggregationPerMonth>>(cache.JsonData);
    }

    /// <summary>
    /// Gets the active users per month.
    /// </summary>
    /// <returns>The active users per month.</returns>

    [HttpGet("countries/{daysInPast}")]
    public async Task<List<AggregationPerValue>> GetCountries(int daysInPast)
    {
        var cache = await _metricDbContext.MetricCache
        .Where(m => m.Type == $"get-countries-job/{daysInPast}")
        .Select(m => new CachedResult(m.JsonData))
        .FirstOrDefaultAsync();

        return JsonConvert.DeserializeObject<List<AggregationPerValue>>(cache.JsonData);

    }

    /// <summary>
    /// Gets the active users per month.
    /// </summary>
    /// <returns>The active users per month.</returns>

    [HttpGet("countries-job/{daysInPast}")]
    public async Task<string> GetCountriesJob(int daysInPast)
    {
        var query = daysInPast == 0 ? SQLRawQueries.getUsersCountriesTotal : SQLRawQueries.getUsersCountriesLastMonth;
        var res = await _metricDbContext.SqlToList<AggregationPerValue>(query, MetricDbContext.AppEditorId, daysInPast);

        var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(res) + $"' WHERE Type = 'get-countries-job/{daysInPast}'";

        await _metricDbContext.Database.ExecuteSqlRawAsync(cacheQuery);

        return "Done";
    }

    /// <summary>
    /// Gets the active users per month.
    /// </summary>
    /// <returns>The active users per month.</returns>

    [HttpGet("active-users-per-day/{minNumberOfLaunch}")]
    public async Task<List<AggregationPerDays>> GetActiveUsersPerDay(int minNumberOfLaunch)
    {
        int editorAppId = MetricDbContext.AppEditorId;
        string metricName = "OpenApplication";
        DateTime date31DaysAgo = DateTime.UtcNow.AddDays(-31);

        var activeUsersPerDay = await _metricDbContext.Metrics
            .Where(a => a.AppId == editorAppId &&
                        a.MetricEventDefinition.MetricName == metricName &&
                        a.Timestamp >= date31DaysAgo)
            .GroupBy(a => new
            {
                a.Timestamp.Year,
                a.Timestamp.Month,
                a.Timestamp.Day,
                a.InstallId
            })
            .Where(g => g.Count() > minNumberOfLaunch)
            .GroupBy(g => new { g.Key.Year, g.Key.Month, g.Key.Day })
            .Select(g => new AggregationPerDays
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Day = g.Key.Day,
                Count = g.Count()
            })
            .OrderBy(aggregation => aggregation.Year)
            .ThenBy(aggregation => aggregation.Month)
            .ThenBy(aggregation => aggregation.Day)
            .ToListAsync();

        return activeUsersPerDay;
    }

    /// <summary>
    /// Gets the usage count per version in the last 30 days.
    /// </summary>
    /// <returns>The usage count per version in the last 30 days.</returns>

    [HttpGet("usage-per-version")]
    public async Task<List<AggregationPerVersion>> GetUsagePerVersion()
    {
        int editorAppId = MetricDbContext.AppEditorId;
        string metricName = "OpenApplication";
        DateTime date30DaysAgo = DateTime.UtcNow.AddDays(-30);

        var usagePerVersion = await _metricDbContext.Metrics
            .Where(a => a.AppId == editorAppId &&
                        a.MetricEventDefinition.MetricName == metricName &&
                        a.Timestamp >= date30DaysAgo)
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
    public async Task<List<AggregationPerMonth>> GetHighUsage()
    {
        var cache = await _metricDbContext.MetricCache
        .Where(m => m.Type == "get-high-usage-job")
        .Select(m => new CachedResult(m.JsonData))
        .FirstOrDefaultAsync();

        return JsonConvert.DeserializeObject<List<AggregationPerMonth>>(cache.JsonData);
    }

    /// <summary>
    /// Gets the usage count per version in the last 30 days.
    /// </summary>
    /// <returns>The usage count per version in the last 30 days.</returns>

    [HttpGet("high-usage-job/{gauge}")]
    public async Task<string> GetHighUsageJob(int gauge)
    {
        var result = await _metricDbContext.SqlToList<AggregationPerMonth>(SQLRawQueries.getUsagePerVersion, MetricDbContext.AppEditorId, gauge);

        var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(result) + "' WHERE Type = 'get-high-usage-job'";

        await _metricDbContext.Database.ExecuteSqlRawAsync(cacheQuery);

        return "Done";
    }


    [HttpGet("stride-downloads")]
    public async Task<List<AggregationPerMonth>> GetStrideDownloads()
    {
        return await _metricDbContext.SqlToList<AggregationPerMonth>(SQLRawQueries.getActiveUsersOfStrideVersionPerMonth, MetricDbContext.AppLauncherId);
    }


    [HttpGet("vsx-downloads")]
    public async Task<List<AggregationPerMonth>> GetVsxDownloads()
    {
        return await _metricDbContext.SqlToList<AggregationPerMonth>(SQLRawQueries.getActiveUsersOfVSXVersionPerMonth, MetricDbContext.AppLauncherId);
    }

    /// <summary>
    /// Gets the crashes count per version in the last 30 days.
    /// </summary>
    /// <returns>The crashes count per version</returns>

    [HttpGet("crashes-per-version")]
    public async Task<List<CrashAggregationResult>> GetCrashesPerVersion()
    {
        var cache = await _metricDbContext.MetricCache
        .Where(m => m.Type == $"get-crashes-per-version-job")
        .Select(m => new CachedResult(m.JsonData))
        .FirstOrDefaultAsync();

        return JsonConvert.DeserializeObject<List<CrashAggregationResult>>(cache.JsonData);

    }

    /// <summary>
    /// Gets the crashes count per version in the last 30 days.
    /// </summary>
    /// <returns>The crashes count per version</returns>

    [HttpGet("crashes-per-version-job")]
    public async Task<string> GetCrashesPerVersionJob()
    {
        var versionAndCrashes = new Dictionary<string, int>();
        var mining = await _metricDbContext.SqlToList<CrashAggregation>(SQLRawQueries.getCrashesPerVersion, MetricDbContext.AppEditorId);
        foreach (var crashAggregation in mining)
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
        var activity = await _metricDbContext.SqlToList<ActivityData>(SQLRawQueries.getActivityData, MetricDbContext.AppEditorId);
        foreach (var activityData in activity)
        {
            versionActivity.Add(activityData.Version, activityData.Time);
        }

        var res = new List<CrashAggregationResult>();
        foreach (var versionAndCrash in versionAndCrashes)
        {
            if (versionActivity.TryGetValue(versionAndCrash.Key, out var time))
            {
                var ratio = versionAndCrash.Value / ((double)time / 60.0 / 60.0);
                res.Add(new CrashAggregationResult(versionAndCrash.Key, ratio));
            }
        }

        var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(res) + "' WHERE Type = 'get-crashes-per-version-job'";

        await _metricDbContext.Database.ExecuteSqlRawAsync(cacheQuery);

        return "Done";
    }

    /// <summary>
    /// Gets the platforms usage
    /// </summary>
    /// <returns>The crashes count per version in the last 30 days.</returns>

    [HttpGet("platforms-usage")]
    public async Task<List<AggregationPerPlatforms>> GetPlatformsUsage()
    {
        // Get active users of version per month

        var result = _metricDbContext.Metrics
            .Join(_metricDbContext.MetricEventDefinitions,
                me => me.MetricId,
                med => med.MetricId,
                (me, med) => new { me, med })
            .Where(x => x.med.MetricName == "OpenSession2" && x.me.Timestamp >= DateTime.Now.AddDays(-31))
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

    [HttpGet("projects-users-job")]
    public async Task<string> GetProjectsUsersJob()
    {
        var res = await _metricDbContext.SqlToList<ProjectsUsersAggregation>(SQLRawQueries.projectsUsersScraper);

        var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(res) + "' WHERE Type = 'get-projects-users-job'";

        await _metricDbContext.Database.ExecuteSqlRawAsync(cacheQuery);

        return "Done";
    }

    /// <summary>
    /// Gets the usage count per version in the last 30 days.
    /// </summary>
    /// <returns>The usage count per version in the last 30 days.</returns>

    [HttpGet("projects-users")]
    public async Task<List<ProjectsUsersAggregation>> GetProjectsUsers()
    {
        var cache = await _metricDbContext.MetricCache
        .Where(m => m.Type == "get-projects-users-job")
        .Select(m => new CachedResult(m.JsonData))
        .FirstOrDefaultAsync();

        return JsonConvert.DeserializeObject<List<ProjectsUsersAggregation>>(cache.JsonData);
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
