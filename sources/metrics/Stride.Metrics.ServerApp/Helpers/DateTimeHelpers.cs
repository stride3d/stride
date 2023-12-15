namespace Stride.Metrics.ServerApp.Helpers;

public static class DateTimeHelpers
{
    public static DateTime NthDaysAgo(uint numOfdays)
    {
        return DateTime.UtcNow.AddDays(-numOfdays);
    }
}