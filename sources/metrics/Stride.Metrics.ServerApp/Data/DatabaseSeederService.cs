using System.Reflection;

using Stride.Metrics.ServerApp.Extensions;
using Stride.Metrics.ServerApp.Models.MetricCreated;

namespace Stride.Metrics.ServerApp.Data;

public class DatabaseSeederService 
{
    private readonly MetricDbContext _metricDbContext;
    public DatabaseSeederService(MetricDbContext metricDbContext)
    {
        _metricDbContext = metricDbContext;
    }
    public void SeedDatabase()
    {
        IEnumerable<FieldInfo> commonAppsGuids = typeof(CommonApps).GetFields()
            .Where(field => field.IsStatic && typeof(MetricAppId).IsAssignableFrom(field.FieldType));

        // Register pre-defined applications
        var metricsApps = commonAppsGuids
            .Select(c => (MetricAppId)c.GetValue(null))
            .Where(m => !_metricDbContext.MetricApps.Any(x => x.AppGuid == m.Guid))
            .Select(m => new MetricApp(m.Guid, m.Name));

        _metricDbContext.MetricApps.AddRange(metricsApps);

        IEnumerable<FieldInfo> metricKeys = typeof(CommonMetrics).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => typeof(MetricKey).IsAssignableFrom(field.FieldType));

        // Register pre-defined metrics
        var metricEventDefinitions = metricKeys
            .Select(c => (MetricKey)c.GetValue(null))
            .Where(m => !_metricDbContext.MetricEventDefinitions.Any(x => x.MetricGuid == m.Guid))
            .Select(m => new MetricEventDefinition(m.Guid, m.Name));

        _metricDbContext.MetricEventDefinitions.AddRange(metricEventDefinitions);

        _metricDbContext.SaveChanges();

        MetricDbContext.AppEditorId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);
        MetricDbContext.AppLauncherId = _metricDbContext.GetApplicationId(CommonApps.StrideLauncherAppId.Guid);
    }
}