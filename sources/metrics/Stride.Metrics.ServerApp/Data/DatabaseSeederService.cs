using System.Globalization;
using System.Reflection;

using Bogus;

using CsvHelper;

using Stride.Metrics.ServerApp.Extensions;
using Stride.Metrics.ServerApp.Models;
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
        FillMetricsApps();
        
        MetricDbContext.AppEditorId = _metricDbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);
        MetricDbContext.AppLauncherId = _metricDbContext.GetApplicationId(CommonApps.StrideLauncherAppId.Guid);

        FillMetricEventDefinitions();
        FillMetricInstall();
        FillMetricEvents();
        FillMetricMarker();
        FillMetricMarkerGroup();
        FillIpToLocations();        
    }

    private void FillMetricEvents()
    {
        if(_metricDbContext.MetricEvents.Any())
            return;

        var fakeEvents = new Faker<MetricEvent>()
                        .RuleFor(r => r.Timestamp, f => f.Date.Between(DateTime.UtcNow.AddYears(-5), DateTime.UtcNow))
                        .RuleFor(r => r.AppId, f => f.Random.Int(0,2))
                        .RuleFor(r => r.InstallId, f => f.Random.Int(0,300))
                        .RuleFor(r => r.IPAddress, f => f.Internet.Ip())
                        .RuleFor(r => r.SessionId, f => f.Random.Int(0,20))
                        .RuleFor(r => r.MetricId, f => f.Random.Int(0,20))
                        .RuleFor(r => r.MetricValue, f => f.Lorem.Text())
                        .RuleFor(r => r.EventId, f => f.Random.Int(0,20))
                        .Generate(300);

        _metricDbContext.MetricEvents.UpdateRange(fakeEvents);
        _metricDbContext.SaveChanges();
    }

    private void FillMetricInstall()
    {
        if(_metricDbContext.MetricInstalls.Any())
            return;

        var fakeInstalls = new Faker<MetricInstall>()
                .RuleFor(r => r.Created, f => f.Date.Between(DateTime.UtcNow.AddYears(-5), DateTime.UtcNow))
                .RuleFor(r => r.InstallGuid, f => f.Random.Guid())
                .Generate(300);

        _metricDbContext.MetricInstalls.UpdateRange(fakeInstalls);
        _metricDbContext.SaveChanges();
    }

    private void FillMetricMarker()
    {
        if(_metricDbContext.MetricMarkers.Any())
            return;

        var fakeMetricMarkers = new Faker<MetricMarker>()
                .RuleFor(r => r.Created, f => f.Date.Between(DateTime.UtcNow.AddYears(-5), DateTime.UtcNow))
                .RuleFor(r => r.Date, f => f.Date.Between(DateTime.UtcNow.AddYears(-5), DateTime.UtcNow))
                .RuleFor(r => r.MarkerGroupId, f => f.Random.Int(0, 3))
                .Generate(300);

        _metricDbContext.MetricMarkers.UpdateRange(fakeMetricMarkers);
        _metricDbContext.SaveChanges();
    }

    private void FillMetricMarkerGroup()
    {
        if(_metricDbContext.MetricMarkerGroups.Any())
            return;

        var fakeMetricMarkerGroups = new Faker<MetricMarkerGroup>()
                .RuleFor(r => r.Name, f => f.Lorem.Text())
                .RuleFor(r => r.Description, f => f.Lorem.Text())
                .Generate(300);

        _metricDbContext.MetricMarkerGroups.UpdateRange(fakeMetricMarkerGroups);
        _metricDbContext.SaveChanges();
    }

    private void FillIpToLocations()
    {
        if(_metricDbContext.IpToLocations.Any())
            return;

        using var reader = new StreamReader("IP2LOCATION-LITE-DB1.csv");
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<IpToLocations>();
        _metricDbContext.IpToLocations.UpdateRange(records);
        _metricDbContext.SaveChanges();
    }

    private void FillMetricEventDefinitions()
    {
        if(_metricDbContext.MetricEventDefinitions.Any())
            return;

        IEnumerable<FieldInfo> metricKeys = typeof(CommonMetrics).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => typeof(MetricKey).IsAssignableFrom(field.FieldType));

        // Register pre-defined metrics
        var metricEventDefinitions = metricKeys
            .Select(c => (MetricKey)c.GetValue(null))
            .Where(m => !_metricDbContext.MetricEventDefinitions.Any(x => x.MetricGuid == m.Guid))
            .Select(m => new MetricEventDefinition(m.Guid, m.Name));

        _metricDbContext.MetricEventDefinitions.AddRange(metricEventDefinitions);
        _metricDbContext.SaveChanges();
    }

    private void FillMetricsApps()
    {
        if(_metricDbContext.MetricApps.Any())
            return;

        IEnumerable<FieldInfo> commonAppsGuids = typeof(CommonApps).GetFields()
                    .Where(field => field.IsStatic && typeof(MetricAppId).IsAssignableFrom(field.FieldType));

        // Register pre-defined applications
        var metricsApps = commonAppsGuids
            .Select(c => (MetricAppId)c.GetValue(null))
            .Where(m => !_metricDbContext.MetricApps.Any(x => x.AppGuid == m.Guid))
            .Select(m => new MetricApp(m.Guid, m.Name));

        _metricDbContext.MetricApps.AddRange(metricsApps);
        _metricDbContext.SaveChanges();
    }
}