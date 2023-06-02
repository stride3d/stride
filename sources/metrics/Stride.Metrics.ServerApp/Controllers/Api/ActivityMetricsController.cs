using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stride.Metrics.ServerApp.Data;
using Stride.Metrics.ServerApp.Dtos;
using Stride.Metrics.ServerApp.Dtos.Agregate;
using Stride.Metrics.ServerApp.Extensions;
using Stride.Metrics.ServerApp.Helpers;
using Stride.Metrics.ServerApp.Models;

namespace Stride.Metrics.ServerApp.Controllers.Api;
///<summary>Activity Install related eps</summary>
[ApiController()]
[Route("api")]
public class ActivityMetricsController
{
    private readonly MetricDbContext _metricDbContext;
    ///<summary>Activity Install related eps</summary>
    public ActivityMetricsController(MetricDbContext metricDbContext)
    {
        _metricDbContext = metricDbContext;
    }

    /// <summary>
    /// xyz
    /// </summary>
    /// <returns>uzs.</returns>

    [HttpGet("high-usage")]
    public List<AggregationPerMonth> GetHighUsage()
    {
        var editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

        var result = _metricDbContext.MetricEvents
                     .Where(ev => ev.MetricEventDefinition.MetricName == "OpenApplication" && ev.AppId == editorAppId)
                     .GroupBy(ev => new
                     {
                         ev.InstallId,
                         ev.Timestamp.Year,
                         ev.Timestamp.Month,
                     })
                     .Select(g => new
                     {
                         g.Key.InstallId,
                         g.Key.Year,
                         g.Key.Month,
                         Total = g.Count()
                     })
                     .GroupBy(g => new { g.Year, g.Month, g.InstallId })
                     .Where(g => g.Sum(m => m.Total) >= 10)
                     .Select(g => new
                     {
                         g.Key.Year,
                         g.Key.Month,
                         Count = g.Sum(m => m.Total),
                         g.Key.InstallId
                     })
                     .GroupBy(g => new { g.Year, g.Month })
                     .OrderBy(g => g.Key.Year)
                        .ThenBy(g => g.Key.Month)
                     .Select(g => new AggregationPerMonth
                     {
                         Month = g.Key.Month,
                         Year = g.Key.Year,
                         Count = g.Count()
                     }).ToList();


        return result;
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
                a.Timestamp.Year,
                a.Timestamp.Month
            })
            .Distinct()
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
    /// Gets the usage count per version in the last 30 days.
    /// </summary>
    /// <returns>The usage count per version in the last 30 days.</returns>

    [HttpGet("usage-per-version")]
    public async Task<List<AggregationPerVersion>> GetUsagePerVersion()
    {
        var editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);

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
    /// Gets the crashes count per version in the last 30 days.
    /// </summary>
    /// <returns>The crashes count per version</returns>

    [HttpGet("crashes-per-version")]
    public List<CrashAggregationResult> GetCrashesPerVersion()
    {
        var editorAppId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);
        //check:
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
            .Where(ab => ab.a.MetricId == ab.b.MetricId
                    && ab.b.MetricName == "OpenApplication"
                    && ab.sq.InstallId == ab.a.InstallId
                    && ab.sq.SessionId == ab.a.SessionId
                    && ab.a.AppId == editorAppId
                    && EF.Functions.DateDiffDay(ab.a.Timestamp, DateTime.Now) < 30)
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
        var query = _metricDbContext.MetricEvents;
                // .Join(
                //     (from a in _metricDbContext.MetricEvents
                //      join b in _metricDbContext.MetricEventDefinitions on a.MetricId equals b.MetricId
                //      where (b.MetricName == "SessionHeartbeat2" || b.MetricName == "CloseSession2")
                //            && EF.Functions.DateDiffDay(a.Timestamp, DateTime.Now) < 30
                //      select new
                //      {
                //          a.InstallId,
                //          a.SessionId,
                //          a.Timestamp.Month,
                //          a.Timestamp.Year,
                //          SessionTime = Convert.ToDecimal(a.MetricValue),
                //          N = SqlFunctions.RowNumber().Over(
                //              PartitionBy(a.InstallId, a.SessionId)
                //                  .OrderByDesc(SqlFunctions.TryCast(a.MetricValue, typeof(decimal?)))
                //                  .ThenBy(a.EventId))
                //      }),
                //     x => new { x.a.InstallId, x.a.SessionId },
                //     y => new { y.InstallId, y.SessionId },
                //     (x, y) => new { x, y })
                // .Where(z => z.y.N == 1 && z.y.SessionTime > 0)
                // .GroupBy(z => new { z.y.Month, z.y.Year, z.y.InstallId, z.y.SessionId })
                // .Select(g => new
                // {
                //     Version = g.Key,
                //     Time = g.Sum(x => x.y.SessionTime)
                // })
                // .Join(
                //     MetricEvents.Join(
                //         MetricEventDefinitions,
                //         a => a.MetricId,
                //         b => b.MetricId,
                //         (a, b) => new { a, b })
                //         .Where(z => z.b.MetricName == "OpenApplication" && z.a.AppId == 0),
                //     x => new { x.Version.InstallId, x.Version.SessionId },
                //     y => new { y.a.InstallId, y.a.SessionId },
                //     (x, y) => new { x, y })
                // .Where(z => z.x.y.SessionId == z.y.a.SessionId && z.x.y.InstallId == z.y.a.InstallId)
                // .GroupBy(z => z.x.Version)
                // .OrderBy(g => g.Key)
                // .Select(g => new
                // {
                //     Version = g.Key,
                //     Time = g.Sum(x => x.x.Time)
                // });


        var versionActivity = new Dictionary<string, decimal>();
        //foreach (var activityData in activity)
        //{
        //    versionActivity.Add(activityData.Version, activityData.Time);
        //}

        var res = new List<CrashAggregationResult>();
        foreach (var versionAndCrash in versionAndCrashes)
        {
            if (versionActivity.TryGetValue(versionAndCrash.Key, out var time))
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
            .Where(x => x.med.MetricName == "OpenSession2" && EF.Functions.DateDiffDay(x.me.Timestamp, DateTime.Now) < 31)
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
}
