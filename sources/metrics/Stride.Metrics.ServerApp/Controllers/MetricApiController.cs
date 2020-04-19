using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Stride.Metrics.ServerApp.Models;

namespace Stride.Metrics.ServerApp.Controllers
{
    //[Authorize]
    [RoutePrefix("api")]
    internal class MetricApiController : CustomApiControllerBase
    {
        // TODO: Generated queries for all get- are a bit invalid in case no events are occuring. We should have a 0 for the days users are not using Stride for example.

        /// <summary>
        /// Use this guid on the client side in the variable StrideMetricsSpecial to avoid loggin usage
        /// </summary>
        private static readonly Guid ByPassSpecialGuid = new Guid("AEA51F92-84DD-40D7-BFE6-442F37A308D6");

        private readonly DateTime dateNow;
        private readonly DateTime date30Days;
        private readonly DateTime date60Days;

        public MetricApiController()
        {
            dateNow = DateTime.UtcNow;
            date30Days = DateTime.UtcNow.AddDays(-30);
            date60Days = DateTime.UtcNow.AddDays(-60);
        }

        /// <summary>
        /// Test API.
        /// </summary>
        /// <returns>IHttpActionResult.</returns>
        [HttpGet]
        [Route("test")]
        public IHttpActionResult Test()
        {
            Trace.TraceInformation("/api/test successfully called");
            return Ok();
        }

        /// <summary>
        /// Gets the installs count since forever.
        /// </summary>
        /// <returns>The installs count.</returns>
        [HttpGet]
        [Route("get-installs-count")]
        public async Task<List<int>> GetInstallsCount()
        {
            using (var db = new MetricDbContext())
            {

                const string getInstallsCount = @"SELECT COUNT(*) as Count FROM [MetricInstalls] a";
                // Get total installs count
                return new List<int>()
                {

                    await db.Database.SqlQuery<int>(getInstallsCount).FirstOrDefaultAsync(),
                    await
                        db.Database.SqlQuery<int>(getInstallsCount + " WHERE a.[Created] < {0}", date30Days)
                            .FirstOrDefaultAsync()
                };
            }
        }

        /// <summary>
        /// Gets the number of active users in the last 30 days. See remarks.
        /// </summary>
        /// <returns>The number of active users in the last 30 days.</returns>
        /// <remarks>An active user is a user launching at least more than 5 times the Editor.</remarks>
        [HttpGet]
        [Route("get-active-users-last-days")]
        public async Task<List<int>> GetActiveUsersLastDays()
        {
            const string getActiveUsersLastDays = @"SELECT COUNT(*) as [Count]
FROM (
SELECT 1 as [Count]
FROM [MetricEvents] as a, [MetricEventDefinitions] as b
WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' AND a.AppId = {{0}}
{0}
GROUP BY a.InstallId
HAVING COUNT(*) > 5
) XX";
            using (var db = new MetricDbContext())
            {
                return new List<int>()
                {
                    // In the past 30 days
                    await db.Database.SqlQuery<int>(string.Format(getActiveUsersLastDays,
                    "AND a.Timestamp > {1}"), MetricDbContext.AppEditorId, date30Days).FirstOrDefaultAsync(),

                    // In the past [-60, -30] days
                    await db.Database.SqlQuery<int>(string.Format(getActiveUsersLastDays,
                    "AND a.Timestamp > {1} AND a.Timestamp <= {2}"), MetricDbContext.AppEditorId, date60Days, date30Days).FirstOrDefaultAsync(),
                };
            }
        }

        public class ActiveUsersView
        {
            public int Month { get; set; }

            public int Year { get; set; }

            public decimal Time { get; set; }

            public int Sessions { get; set; }

            public int Users { get; set; }
        }

        public class CachedResult
        {
            public string JsonData { get; set; }
        }

        /// <summary>
        /// Gets the number of active users in the last 30 days. See remarks.
        /// </summary>
        /// <returns>The number of active users in the last 30 days.</returns>
        /// <remarks>An active user is a user launching at least more than 5 times the Editor.</remarks>
        [HttpGet]
        [Route("get-active-users")]
        public async Task<List<ActiveUsersView>> GetActiveUsers()
        {
            const string getActiveUsers = "SELECT JsonData from [MetricCache] WHERE Type = 'get-active-users-job'";           
            var cache = await SqlToList<CachedResult>(getActiveUsers);
            return JsonConvert.DeserializeObject<List<ActiveUsersView>>(cache[0].JsonData);
        }

        /// <summary>
        /// Gets the number of active users in the last 30 days. See remarks.
        /// </summary>
        /// <returns>The number of active users in the last 30 days.</returns>
        /// <remarks>An active user is a user launching at least more than 5 times the Editor.</remarks>
        [HttpGet]
        [Route("get-active-users-job")]
        public async Task<string> GetActiveUsersJob()
        {
            const string getActiveUsers = @"
SELECT MONTH(Timestamp) as Month, YEAR(Timestamp) as Year, SUM(SessionTime) as Time, COUNT(InstallId) as Sessions, COUNT(DISTINCT InstallId) as Users FROM (
SELECT DISTINCT InstallId, SessionId, TRY_CAST(MetricValue AS DECIMAL(18,2)) as SessionTime, Timestamp, RANK () OVER (PARTITION BY InstallId, SessionId ORDER BY TRY_CAST(MetricValue AS DECIMAL(18,2)) DESC, EventId) as N 
FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b
WHERE 
a.MetricId = b.MetricId and b.MetricName = 'SessionHeartbeat2' or a.MetricId = b.MetricId and b.MetricName = 'CloseSession2')M 
WHERE N = 1 AND SessionTime > 0
GROUP BY MONTH(Timestamp), YEAR(Timestamp), N
ORDER BY YEAR(Timestamp), MONTH(Timestamp)
";

            var result = await SqlToList<ActiveUsersView>(getActiveUsers);

            var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(result) + "' WHERE Type = 'get-active-users-job'";

            using (var db = new MetricDbContext())
            {
                await db.Database.ExecuteSqlCommandAsync(cacheQuery);
            }

            return "Done";
        }

        /// <summary>
        /// Gets the installs count per month.
        /// </summary>
        /// <returns>An installs count per month.</returns>
        [HttpGet]
        [Route("get-installs-per-month")]
        public async Task<List<AggregationPerMonth>> GetInstallsPerMonth()
        {
            // Get installations per month
            const string getInstallsPerMonth =
                @"select DATEPART(year, a.Created) as [Year], DATEPART(month, a.Created) as [Month], count(*) as [Count]
from [MetricInstalls] as a
group BY DATEPART(year, a.Created), DATEPART(month, a.Created)
order BY DATEPART(year, a.Created), DATEPART(month, a.Created)";

            return await SqlToList<AggregationPerMonth>(getInstallsPerMonth);
        }

        public class YearProjection
        {
            public int January { get; set; }
            public int February { get; set; }
            public int March { get; set; }
            public int April { get; set; }
            public int May { get; set; }
            public int June { get; set; }
            public int July { get; set; }
            public int August { get; set; }
            public int September { get; set; }
            public int October { get; set; }
            public int November { get; set; }
            public int December { get; set; }
            public int Count { get; set; }
            public int Year { get; set; }
        }

        /// <summary>
        /// Gets the installs count per month.
        /// </summary>
        /// <returns>An installs count per month.</returns>
        [HttpGet]
        [Route("get-installs-trend")]
        public async Task<List<YearProjection>> GetInstallsPerMonth2()
        {
            // Get installations per month
            const string getInstallsPerMonth =
                @"SELECT 
SUM(CASE MONTH(Created) WHEN 1 THEN 1 ELSE 0 END) as January,
SUM(CASE MONTH(Created) WHEN 2 THEN 1 ELSE 0 END) as February,
SUM(CASE MONTH(Created) WHEN 3 THEN 1 ELSE 0 END) as March,
SUM(CASE MONTH(Created) WHEN 4 THEN 1 ELSE 0 END) as April,
SUM(CASE MONTH(Created) WHEN 5 THEN 1 ELSE 0 END) as May,
SUM(CASE MONTH(Created) WHEN 6 THEN 1 ELSE 0 END) as June, 
SUM(CASE MONTH(Created) WHEN 7 THEN 1 ELSE 0 END) as July,
SUM(CASE MONTH(Created) WHEN 8 THEN 1 ELSE 0 END) as August,
SUM(CASE MONTH(Created) WHEN 9 THEN 1 ELSE 0 END) as September, 
SUM(CASE MONTH(Created) WHEN 10 THEN 1 ELSE 0 END) as October, 
SUM(CASE MONTH(Created) WHEN 11 THEN 1 ELSE 0 END) as November, 
SUM(CASE MONTH(Created) WHEN 12 THEN 1 ELSE 0 END) as December,
COUNT(Created) as Count,
YEAR(Created) as Year
FROM [MetricInstalls]
GROUP BY YEAR(Created)
ORDER BY YEAR(Created)
";

            var result = await SqlToList<YearProjection>(getInstallsPerMonth);

            var pastCount = 0;
            foreach (var yearProjection in result)
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

            return result;
        }

        /// <summary>
        /// Gets the installs count in the last 30 days per day.
        /// </summary>
        /// <returns>The installs count in the last 30 days per day</returns>
        [HttpGet]
        [Route("get-installs-last-days")]
        public async Task<List<AggregationPerDay>> GetInstallLastDays()
        {
            // Get installations per day in the last 30 days
            const string getInstallsLast30Days = @"select cast(a.Created as Date) as [Date], count(*) as [Count]
from [MetricInstalls] as a
where a.Created > {0}
group BY CAST(a.Created as Date)
order BY CAST(a.Created as Date)";

            return await SqlToList<AggregationPerDay>(getInstallsLast30Days, date30Days);
        }

        /// <summary>
        /// Gets the active users per month.
        /// </summary>
        /// <returns>The active users per month.</returns>
        [HttpGet]
        [Route("get-active-users-per-month/{minNumberOfLaunch}")]
        public async Task<List<AggregationPerMonth>> GetActiveUsersPerMonth(int minNumberOfLaunch)
        {
            // Get active users per month
            const string getActiveUsersPerMonth = @"SELECT [Year], [Month], count(*) as [Count]
FROM (
SELECT DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month]
FROM [MetricEvents] as a, [MetricEventDefinitions] as b
WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' AND a.AppId = {0}
GROUP BY DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), a.InstallId
HAVING COUNT(*) > {1}
) UsersRunningMoreThan5TimesDuringOneMonth
GROUP BY [Year], [Month]
ORDER BY [Year], [Month]";

            return await SqlToList<AggregationPerMonth>(getActiveUsersPerMonth, MetricDbContext.AppEditorId, minNumberOfLaunch);
        }

        private class AggregationPerInstall
        {
            public int Year { get; set; }

            public int Month { get; set; }

            public int InstallId { get; set; }
        }

        /// <summary>
        /// Gets the active users per month.
        /// </summary>
        /// <returns>The active users per month.</returns>
        [HttpGet]
        [Route("get-quitting-count-job")]
        public async Task<string> GetQuittingCountJob()
        {
            // Get active users per month
            const string getActiveUsersPerMonth = @"
SELECT [Year], [Month], InstallId
FROM (
SELECT DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month], InstallId
FROM [MetricEvents] as a, [MetricEventDefinitions] as b
WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' AND a.AppId = {0}
GROUP BY DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), a.InstallId
HAVING COUNT(*) > 0
) N
GROUP BY [Year], [Month], InstallId
ORDER BY [Year], [Month]
";

            var installations = await SqlToList<AggregationPerInstall>(getActiveUsersPerMonth, MetricDbContext.AppEditorId);
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

            using (var db = new MetricDbContext())
            {
                await db.Database.ExecuteSqlCommandAsync(cacheQuery);
            }

            return "Done";
        }

        /// <summary>
        /// Gets the active users per month.
        /// </summary>
        /// <returns>The active users per month.</returns>
        [HttpGet]
        [Route("get-quitting-count")]
        public async Task<List<AggregationPerMonth>> GetQuittingCount()
        {
            string getActiveUsers = $"SELECT JsonData from [MetricCache] WHERE Type = 'get-quitting-count-job'";
            var cache = await SqlToList<CachedResult>(getActiveUsers);
            return JsonConvert.DeserializeObject<List<AggregationPerMonth>>(cache[0].JsonData);
        }

        private const string getUsersCountriesTotal = @"SELECT TOP(15) count(*) as [Count], [Value]
FROM (
SELECT dbo.IPAddressToCountry(a.IPAddress) as Value
FROM [MetricEvents] as a, [MetricEventDefinitions] as b
WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' AND a.AppId = {0}
GROUP BY a.InstallId, a.IPAddress
) M
WHERE Value != '-'
GROUP BY [Value]
HAVING count(*) > 5 
ORDER BY count(*) DESC, [Value]";

        private const string getUsersCountriesLastMonth = @"SELECT TOP(15) count(*) as [Count], [Value]
FROM (
SELECT dbo.IPAddressToCountry(a.IPAddress) as Value
FROM [MetricEvents] as a, [MetricEventDefinitions] as b
WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' AND a.AppId = {0} AND DATEDIFF(day ,a.Timestamp, CURRENT_TIMESTAMP) < {1}
GROUP BY a.InstallId, a.IPAddress
) M
WHERE Value != '-'
GROUP BY [Value]
HAVING count(*) > 5 
ORDER BY count(*) DESC, [Value]";

        /// <summary>
        /// Gets the active users per month.
        /// </summary>
        /// <returns>The active users per month.</returns>
        [HttpGet]
        [Route("get-countries/{daysInPast}")]
        public async Task<List<AggregationPerValue>> GetCountries(int daysInPast)
        {
            string getActiveUsers = $"SELECT JsonData from [MetricCache] WHERE Type = 'get-countries-job/{daysInPast}'";
            var cache = await SqlToList<CachedResult>(getActiveUsers);
            return JsonConvert.DeserializeObject<List<AggregationPerValue>>(cache[0].JsonData);
        }

        /// <summary>
        /// Gets the active users per month.
        /// </summary>
        /// <returns>The active users per month.</returns>
        [HttpGet]
        [Route("get-countries-job/{daysInPast}")]
        public async Task<string> GetCountriesJob(int daysInPast)
        {
            var query = daysInPast == 0 ? getUsersCountriesTotal : getUsersCountriesLastMonth;
            var res = await SqlToList<AggregationPerValue>(query, MetricDbContext.AppEditorId, daysInPast);

            var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(res) + $"' WHERE Type = 'get-countries-job/{daysInPast}'";

            using (var db = new MetricDbContext())
            {
                await db.Database.ExecuteSqlCommandAsync(cacheQuery);
            }

            return "Done";
        }

        /// <summary>
        /// Gets the active users per month.
        /// </summary>
        /// <returns>The active users per month.</returns>
        [HttpGet]
        [Route("get-active-users-per-day/{minNumberOfLaunch}")]
        public async Task<List<AggregationPerDays>> GetActiveUsersPerDay(int minNumberOfLaunch)
        {
            // Get active users per month
            const string getActiveUsersPerDay = @"SELECT [Year], [Month], [Day], count(*) as [Count]
FROM (
SELECT DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month], DATEPART(day, a.Timestamp) as [Day]
FROM [MetricEvents] as a, [MetricEventDefinitions] as b
WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' AND a.AppId = {0} AND DATEDIFF(day ,a.Timestamp, CURRENT_TIMESTAMP) < 31
GROUP BY DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), DATEPART(day, a.Timestamp), a.InstallId
HAVING COUNT(*) > {1}
) UsersRunningMoreThan5TimesDuringOneMonth
GROUP BY [Year], [Month], [Day]
ORDER BY [Year], [Month], [Day]";

            return await SqlToList<AggregationPerDays>(getActiveUsersPerDay, MetricDbContext.AppEditorId, minNumberOfLaunch);
        }

        /// <summary>
        /// Gets the usage count per version in the last 30 days.
        /// </summary>
        /// <returns>The usage count per version in the last 30 days.</returns>
        [HttpGet]
        [Route("get-usage-per-version")]
        public async Task<List<AggregationPerVersion>> GetUsagePerVersion()
        {
            // Get active users of version per month
            const string getUsagePerVersion = @"SELECT a.MetricValue as [Version], COUNT(DISTINCT a.InstallId) AS [Count]
FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b
WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' AND a.AppId = {0}
AND a.Timestamp > {1}
GROUP BY a.MetricValue
ORDER BY a.MetricValue";

            return await SqlToList<AggregationPerVersion>(getUsagePerVersion, MetricDbContext.AppEditorId, date30Days);
        }

        /// <summary>
        /// Gets the usage count per version in the last 30 days.
        /// </summary>
        /// <returns>The usage count per version in the last 30 days.</returns>
        [HttpGet]
        [Route("get-high-usage")]
        public async Task<List<AggregationPerMonth>> GetHighUsage()
        {
            const string getActiveUsers = "SELECT JsonData from [MetricCache] WHERE Type = 'get-high-usage-job'";
            var cache = await SqlToList<CachedResult>(getActiveUsers);
            return JsonConvert.DeserializeObject<List<AggregationPerMonth>>(cache[0].JsonData);
        }

        /// <summary>
        /// Gets the usage count per version in the last 30 days.
        /// </summary>
        /// <returns>The usage count per version in the last 30 days.</returns>
        [HttpGet]
        [Route("get-high-usage-job/{gauge}")]
        public async Task<string> GetHighUsageJob(int gauge)
        {
            // Get active users of version per month
            const string getUsagePerVersion = @"SELECT N.Month, N.Year, COUNT(N.InstallId) as Count
FROM(
SELECT M.Year, M.Month, SUM(M.Total) as Count, M.InstallId
FROM(
SELECT a.InstallId, DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month], DATEPART(day, a.Timestamp) as [Day],
RANK() OVER (PARTITION BY a.InstallId, DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp) ORDER BY DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), a.InstallId) as Total
FROM [MetricEvents] as a, [MetricEventDefinitions] as b
WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' AND a.AppId = {0}
GROUP BY a.InstallId, DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), DATEPART(day, a.Timestamp)
)M
GROUP BY M.Year, M.Month, M.InstallId
HAVING SUM(M.Total) >= {1}
)N
GROUP BY N.Year, N.Month
ORDER BY N.Year, N.Month";

            var result = await SqlToList<AggregationPerMonth>(getUsagePerVersion, MetricDbContext.AppEditorId, gauge);

            var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(result) + "' WHERE Type = 'get-high-usage-job'";

            using (var db = new MetricDbContext())
            {
                await db.Database.ExecuteSqlCommandAsync(cacheQuery);
            }

            return "Done";
        }

        [HttpGet]
        [Route("get-stride-downloads")]
        public async Task<List<AggregationPerMonth>> GetStrideDownloads()
        {
            // Get active users of version per month
            const string query = @"SELECT [Year], [Month], count(*) as [Count]
FROM (
SELECT DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month]
FROM [MetricEvents] as a, [MetricEventDefinitions] as b
WHERE a.MetricId = b.MetricId and b.MetricName = 'DownloadPackage' AND a.AppId = {0} AND CHARINDEX('Stride', a.MetricValue) = 1 AND CHARINDEX('DownloadCompleted', a.MetricValue) >= 1
GROUP BY DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), a.InstallId
) Q
GROUP BY [Year], [Month]
ORDER BY [Year], [Month]";

            return await SqlToList<AggregationPerMonth>(query, MetricDbContext.AppLauncherId);
        }

        [HttpGet]
        [Route("get-vsx-downloads")]
        public async Task<List<AggregationPerMonth>> GetVsxDownloads()
        {
            // Get active users of version per month
            const string query = @"SELECT [Year], [Month], count(*) as [Count]
FROM (
SELECT DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month]
FROM [MetricEvents] as a, [MetricEventDefinitions] as b
WHERE a.MetricId = b.MetricId and b.MetricName = 'DownloadPackage' AND a.AppId = {0} AND CHARINDEX('VisualStudio', a.MetricValue) >= 1 AND CHARINDEX('DownloadCompleted', a.MetricValue) >= 1
GROUP BY DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), a.InstallId
) Q
GROUP BY [Year], [Month]
ORDER BY [Year], [Month]";

            return await SqlToList<AggregationPerMonth>(query, MetricDbContext.AppLauncherId);
        }

        public class CrashAggregation
        {
            public int VersionId { get; set; }

            public int SessionId { get; set; }

            public string MetricSent { get; set; }

            public string Version { get; set; }

            public int Appid { get; set; }
        }

        public class CrashAggregationResult
        {
            public string Version { get; set; }
            
            public double Ratio { get; set; }
        }

        public class ActivityData
        {
            public string Version { get; set; }

            public decimal Time { get; set; }
        }

        /// <summary>
        /// Gets the crashes count per version in the last 30 days.
        /// </summary>
        /// <returns>The crashes count per version</returns>
        [HttpGet]
        [Route("get-crashes-per-version")]
        public async Task<List<CrashAggregationResult>> GetCrashesPerVersion()
        {
            const string getActiveUsers = "SELECT JsonData from [MetricCache] WHERE Type = 'get-crashes-per-version-job'";
            var cache = await SqlToList<CachedResult>(getActiveUsers);
            return JsonConvert.DeserializeObject<List<CrashAggregationResult>>(cache[0].JsonData);
        }

        /// <summary>
        /// Gets the crashes count per version in the last 30 days.
        /// </summary>
        /// <returns>The crashes count per version</returns>
        [HttpGet]
        [Route("get-crashes-per-version-job")]
        public async Task<string> GetCrashesPerVersionJob()
        {
            const string getCrashesPerVersion = @"
SELECT sq.InstallId, sq.SessionId, sq.MetricSent, MetricValue as Version
FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b, (SELECT InstallId, SessionId, MetricValue as MetricSent FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b WHERE a.MetricId = b.MetricId and b.MetricName = 'CrashedSession') as sq
WHERE 
a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' and sq.InstallId = a.InstallId and sq.SessionId = a.SessionId AND a.AppId = {0} AND DATEDIFF(day ,a.Timestamp, CURRENT_TIMESTAMP) < 30
GROUP BY sq.InstallId, sq.SessionId, MetricValue, sq.MetricSent
ORDER BY Version
";

            const string getActivityData = @"
SELECT MetricValue as Version, SUM(Time) as Time FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b, (SELECT InstallId, SessionId, MONTH(Timestamp) as Month, YEAR(Timestamp) as Year, SUM(SessionTime) as Time FROM (
SELECT DISTINCT a.InstallId, a.SessionId, TRY_CAST(MetricValue AS DECIMAL(18,2)) as SessionTime, Timestamp, RANK () OVER (PARTITION BY InstallId, SessionId ORDER BY TRY_CAST(MetricValue AS DECIMAL(18,2)) DESC, EventId) as N
FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b
WHERE 
a.MetricId = b.MetricId and b.MetricName = 'SessionHeartbeat2' or a.MetricId = b.MetricId and b.MetricName = 'CloseSession2' AND DATEDIFF(day ,a.Timestamp, CURRENT_TIMESTAMP) < 30) as M
WHERE N = 1 AND SessionTime > 0
GROUP BY MONTH(Timestamp), YEAR(Timestamp), InstallId, SessionId) as Activity
WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' and Activity.SessionId = a.SessionId and Activity.InstallId = a.InstallId and a.AppId = {0}
GROUP BY MetricValue
ORDER BY Version
";

            var versionAndCrashes = new Dictionary<string, int>();
            var mining = await SqlToList<CrashAggregation>(getCrashesPerVersion, MetricDbContext.AppEditorId);
            foreach (var crashAggregation in mining)
            {
                if(crashAggregation.Version.StartsWith("1.20")) continue;
                if(crashAggregation.Version.Contains("alpha")) continue;
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
            var activity = await SqlToList<ActivityData>(getActivityData, MetricDbContext.AppEditorId);
            foreach (var activityData in activity)
            {
                versionActivity.Add(activityData.Version, activityData.Time);
            }

            var res = new List<CrashAggregationResult>();
            foreach (var versionAndCrash in versionAndCrashes)
            {
                decimal time;
                if (versionActivity.TryGetValue(versionAndCrash.Key, out time))
                {
                    var ratio = versionAndCrash.Value/((double)time / 60.0 / 60.0);
                    res.Add(new CrashAggregationResult { Version = versionAndCrash.Key, Ratio = ratio });
                }
            }

            var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(res) + "' WHERE Type = 'get-crashes-per-version-job'";

            using (var db = new MetricDbContext())
            {
                await db.Database.ExecuteSqlCommandAsync(cacheQuery);
            }

            return "Done";
        }

        /// <summary>
        /// Gets the platforms usage
        /// </summary>
        /// <returns>The crashes count per version in the last 30 days.</returns>
        [HttpGet]
        [Route("get-platforms-usage")]
        public async Task<List<AggregationPerPlatforms>> GetPlatformsUsage()
        {
            // Get active users of version per month
            const string getPlatformsUsage = @"SELECT a.MetricValue FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenSession2' AND DATEDIFF(day ,a.Timestamp, CURRENT_TIMESTAMP) < 31";

            var res = await SqlToList<string>(getPlatformsUsage, MetricDbContext.AppEditorId);

            var dict = new Dictionary<string, int>();
            foreach (var val in res)
            {
                var split = val.Split('|', '&');
                foreach (var v in split)
                {
                    if (v.StartsWith("#platform:"))
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
            }

            var sortedValues = dict.OrderBy(x => x.Value).ToDictionary(x => x.Key, y => y.Value);

            return sortedValues.Keys.Select(key => new AggregationPerPlatforms {Platform = key, Count = dict[key]}).ToList();
        }

        public class ProjectsUsersAggregation
        {
            public int Year { get; set; }

            public int Month { get; set; }

            public int MoreThan1 { get; set; }

            public int MoreThan3 { get; set; }

            public int MoreThan5 { get; set; }
        }

        private const string projectsUsersScraper = @"
SELECT 
SUM(IIF(Count > 5, 1, 0)) as MoreThan5,
SUM(IIF(Count > 3, 1, 0)) as MoreThan3,
SUM(IIF(Count > 1, 1, 0)) as MoreThan1,
Year,
Month
FROM(
SELECT COUNT(ProjectUUID) as Count, ProjectUUID, MetricValue, Month, Year
FROM(
SELECT DISTINCT SUBSTRING(MetricValue, 13, 36) as ProjectUUID, InstallId, MetricValue, DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month] FROM [MetricEvents] as a WHERE [MetricId] = 19
)N
WHERE N.ProjectUUID != '00000000-0000-0000-0000-000000000000'
GROUP BY Month, Year, ProjectUUID, MetricValue
HAVING COUNT(ProjectUUID) > 1
)M
GROUP BY Month, Year
";

        /// <summary>
        /// Gets the usage count per version in the last 30 days.
        /// </summary>
        /// <returns>The usage count per version in the last 30 days.</returns>
        [HttpGet]
        [Route("get-projects-users-job")]
        public async Task<string> GetProjectsUsersJob()
        {
            var res = await SqlToList<ProjectsUsersAggregation>(projectsUsersScraper);

            var cacheQuery = "UPDATE [MetricCache] SET JsonData = '" + JsonConvert.SerializeObject(res) + "' WHERE Type = 'get-projects-users-job'";

            using (var db = new MetricDbContext())
            {
                await db.Database.ExecuteSqlCommandAsync(cacheQuery);
            }

            return "Done";
        }

        /// <summary>
        /// Gets the usage count per version in the last 30 days.
        /// </summary>
        /// <returns>The usage count per version in the last 30 days.</returns>
        [HttpGet]
        [Route("get-projects-users")]
        public async Task<List<ProjectsUsersAggregation>> GetProjectsUsers()
        {
            const string getActiveUsers = "SELECT JsonData from [MetricCache] WHERE Type = 'get-projects-users-job'";
            var cache = await SqlToList<CachedResult>(getActiveUsers);
            return JsonConvert.DeserializeObject<List<ProjectsUsersAggregation>>(cache[0].JsonData);
        }

        /// <summary>
        /// Pushes the specified new metric.
        /// </summary>
        /// <param name="newMetric">The new metric.</param>
        /// <returns>Ok() unless an error occurs</returns>
        [HttpPost]
        [Route("push-metric")]
        public IHttpActionResult Push(NewMetricMessage newMetric)
        {
            // If special guid, then don't store anything
            var ipAddress = GetIPAddress();

            if (newMetric.SpecialId == ByPassSpecialGuid) //filter out SSKK by default
            {
                Trace.TraceInformation("/api/push-metric (origin: {0}) with SpecialGuid not saved {1}", ipAddress, JsonConvert.SerializeObject(newMetric));
                return Ok();
            }

            // TODO: Disable pooling for now to make sure that we don't starve global ThreadPool as it is used also by ASP.net
            // Put this immediately in a worker thread
            //ThreadPool.QueueUserWorkItem(state =>
            //{
            var clock = Stopwatch.StartNew();
            MetricEvent result;
            using (var db = new MetricDbContext())
            {
                result = db.SaveNewMetric(newMetric, ipAddress);
            }
            Trace.TraceInformation("/api/push-metric New metric saved in {0}ms: {1}", clock.ElapsedMilliseconds, JsonConvert.SerializeObject(result));
            //});

            return Ok();
        }

        //[Authorize(Roles = "Admin")]
        //[HttpGet]
        //[Route("api/test")]
        //public IHttpActionResult TestAdmin()
        //{
        //    var caller = User as ClaimsPrincipal;

        //    return Ok();
        //}
    }
}