namespace Stride.Metrics.ServerApp.Data;

public static class SQLMetricQuery
{
    public static string HighUsage(int editorAppId) => $@"SELECT N.Month, N.Year, COUNT(N.InstallId) as Count
                    FROM(
                    SELECT M.Year, M.Month, SUM(M.Total) as Count, M.InstallId
                    FROM(
                    SELECT a.InstallId, DATEPART(year, a.Timestamp) as [Year], DATEPART(month, a.Timestamp) as [Month], DATEPART(day, a.Timestamp) as [Day],
                    RANK() OVER (PARTITION BY a.InstallId, DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp) ORDER BY DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), a.InstallId) as Total
                    FROM [MetricEvents] as a, [MetricEventDefinitions] as b
                    WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' AND a.AppId = {editorAppId}
                    GROUP BY a.InstallId, DATEPART(year, a.Timestamp), DATEPART(month, a.Timestamp), DATEPART(day, a.Timestamp)
                    )M
                    GROUP BY M.Year, M.Month, M.InstallId
                    HAVING SUM(M.Total) >= 10
                    )N
                    GROUP BY N.Year, N.Month
                    ORDER BY N.Year, N.Month";

    public static string ActivityData(int editorAppId) => $@"SELECT MetricValue as Version, SUM(Time) as Time FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b, (SELECT InstallId, SessionId, MONTH(Timestamp) as Month, YEAR(Timestamp) as Year, SUM(SessionTime) as Time FROM (
                    SELECT DISTINCT a.InstallId, a.SessionId, TRY_CAST(MetricValue AS DECIMAL(18,2)) as SessionTime, Timestamp, RANK () OVER (PARTITION BY InstallId, SessionId ORDER BY TRY_CAST(MetricValue AS DECIMAL(18,2)) DESC, EventId) as N
                    FROM [MetricEvents] AS a, [MetricEventDefinitions] AS b
                    WHERE 
                    a.MetricId = b.MetricId and b.MetricName = 'SessionHeartbeat2' or a.MetricId = b.MetricId and b.MetricName = 'CloseSession2' AND DATEDIFF(day ,a.Timestamp, CURRENT_TIMESTAMP) < 30) as M
                    WHERE N = 1 AND SessionTime > 0
                    GROUP BY MONTH(Timestamp), YEAR(Timestamp), InstallId, SessionId) as Activity
                    WHERE a.MetricId = b.MetricId and b.MetricName = 'OpenApplication' and Activity.SessionId = a.SessionId and Activity.InstallId = a.InstallId and a.AppId = {0}
                    GROUP BY MetricValue
                    ORDER BY Version";
}