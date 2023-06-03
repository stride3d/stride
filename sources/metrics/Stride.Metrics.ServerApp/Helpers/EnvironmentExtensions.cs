namespace Stride.Metrics.ServerApp.Helpers;

public static class EnvironmentHelpers
{
    ///<summary>
    /// Enables database seeding if developer provides <c>--SeedMetricsData</c> parameter i.e. using <c>dotnet run</c> command
    ///</summary>
    public static bool IsSeedingEnabled()
    {
        return Environment.GetCommandLineArgs().Any(a => a == "--SeedMetricsData");
    }
}