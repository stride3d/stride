namespace Stride.Metrics.ServerApp.Data;

public static class SQLRawQueries
{
    public static string getActiveUsers = @"
        SELECT MONTH(Timestamp) as Month, YEAR(Timestamp) as Year, SUM(SessionTime) as Time, COUNT(InstallId) as Sessions, COUNT(DISTINCT InstallId) as Users FROM (
        SELECT DISTINCT InstallId, SessionId, TRY_CAST(MetricValue AS DECIMAL(18,2)) as SessionTime, Timestamp, RANK () OVER (PARTITION BY InstallId, SessionId ORDER BY TRY_CAST(MetricValue AS DECIMAL(18,2)) DESC, EventId) as N 
        FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b
        WHERE 
        a.MetricId = b.MetricId and b.MetricName = 'SessionHeartbeat2' or a.MetricId = b.MetricId and b.MetricName = 'CloseSession2')M 
        WHERE N = 1 AND SessionTime > 0
        GROUP BY MONTH(Timestamp), YEAR(Timestamp), N
        ORDER BY YEAR(Timestamp), MONTH(Timestamp)
        ";
    public static string getActiveUsersPerMonth = @"
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
    public static string getUsersCountriesTotal = @"
        SELECT TOP(15) count(*) as [Count], [Value]
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

    public static string getUsersCountriesLastMonth = @"
        SELECT TOP(15) count(*) as [Count], [Value]
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

    public static string getUsagePerVersion = @"
        SELECT N.Month, N.Year, COUNT(N.InstallId) as Count
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

    public static string getActiveUsersOfStrideVersionPerMonth = @"
        SELECT [Year], [Month], count(*) as [Count]
        FROM (
        SELECT DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month]
        FROM [MetricEvents] as a, [MetricEventDefinitions] as b
        WHERE a.MetricId = b.MetricId and b.MetricName = 'DownloadPackage' AND a.AppId = {0} AND CHARINDEX('Stride', a.MetricValue) = 1 AND CHARINDEX('DownloadCompleted', a.MetricValue) >= 1
        GROUP BY DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), a.InstallId
        ) Q
        GROUP BY [Year], [Month]
        ORDER BY [Year], [Month]";

    public static string getActiveUsersOfVSXVersionPerMonth = @"
        SELECT [Year], [Month], count(*) as [Count]
        FROM (
        SELECT DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month]
        FROM [MetricEvents] as a, [MetricEventDefinitions] as b
        WHERE a.MetricId = b.MetricId and b.MetricName = 'DownloadPackage' AND a.AppId = {0} AND CHARINDEX('VisualStudio', a.MetricValue) >= 1 AND CHARINDEX('DownloadCompleted', a.MetricValue) >= 1
        GROUP BY DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), a.InstallId
        ) Q
        GROUP BY [Year], [Month]
        ORDER BY [Year], [Month]";

    public static string getCrashesPerVersion = @"
        SELECT sq.InstallId, sq.SessionId, sq.MetricSent, MetricValue as Version
        FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b, (SELECT InstallId, SessionId, MetricValue as MetricSent FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b WHERE a.MetricId = b.MetricId and b.MetricName = 'CrashedSession') as sq
        WHERE 
        a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' and sq.InstallId = a.InstallId and sq.SessionId = a.SessionId AND a.AppId = {0} AND DATEDIFF(day ,a.Timestamp, CURRENT_TIMESTAMP) < 30
        GROUP BY sq.InstallId, sq.SessionId, MetricValue, sq.MetricSent
        ORDER BY Version
        ";

    public static string getActivityData = @"
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
    public static string projectsUsersScraper = @"
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
}
