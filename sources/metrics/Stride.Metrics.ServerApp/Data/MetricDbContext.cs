using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Stride.Metrics.ServerApp.Models;
using Stride.Metrics.ServerApp.Models.MetricCreated;

namespace Stride.Metrics.ServerApp.Data;

public class MetricDbContext : DbContext
{
    public MetricDbContext(DbContextOptions<MetricDbContext> options)
        : base(options)
    {
    }

    public DbSet<MetricEvent> Metrics { get; set; }

    public DbSet<MetricApp> Apps { get; set; }

    public DbSet<MetricEventDefinition> MetricDefinitions { get; set; }

    public DbSet<MetricInstall> Installs { get; set; }

    public DbSet<MetricMarker> Markers { get; set; }

    public DbSet<MetricMarkerGroup> MarkerGroups { get; set; }

    public DbSet<MetricCache> MetricCache { get; set; }

    public DbSet<MetricEventDefinition> MetricEventDefinitions { get; set; }
    public DbSet<IpToLocations> IpToLocations { get; set; }

    public static int AppEditorId { get; private set; }

    public static int AppLauncherId { get; private set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MetricApp>()
            .HasIndex(a => a.AppName)
            .IsUnique();

        modelBuilder.Entity<MetricApp>()
            .HasIndex(a => a.AppGuid)
            .IsUnique();
        
        modelBuilder.Entity<MetricApp>()
            .ToTable("MetricApps");

        modelBuilder.Entity<MetricEventDefinition>()
           .HasIndex(a => a.MetricGuid)
           .IsUnique();

        modelBuilder.Entity<MetricEventDefinition>()
           .HasIndex(a => a.MetricName)
           .IsUnique();

        modelBuilder.Entity<MetricEventDefinition>()
           .ToTable("MetricEventDefinitions");

        modelBuilder.Entity<MetricInstall>()
           .HasIndex(a => a.InstallGuid)
           .IsUnique();
        
        modelBuilder.Entity<MetricInstall>()
           .ToTable("MetricMarkerGroups");
        
        modelBuilder.Entity<MetricMarker>()
           .ToTable("MetricMarkers");

        modelBuilder.Entity<MetricEvent>()
            .HasKey(m => new { m.Timestamp, m.AppId, m.InstallId, m.SessionId, m.MetricId });
        
        modelBuilder.Entity<MetricEvent>()
            .Property(m => m.Timestamp)
            .HasColumnType("datetime2");
        
        modelBuilder.Entity<MetricEvent>()
            .ToTable("MetricEvents");
        
        modelBuilder.Entity<IpToLocations>()
            .HasIndex(a => a.IpFrom)
            .IsUnique();

        modelBuilder.Entity<IpToLocations>()
            .HasIndex(a => a.IpTo)
            .IsUnique();
    }

    internal static void Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MetricDbContext>();

        // Register pre-defined applications
        foreach (var metricAppField in typeof(CommonApps).GetFields()
            .Where(field => field.IsStatic && typeof(MetricAppId).IsAssignableFrom(field.FieldType)))
        {
            var metricAppId = (MetricAppId)metricAppField.GetValue(null);
            if (dbContext.Apps.Any(x => x.AppGuid == metricAppId.Guid)) continue;
            dbContext.Apps.Add(new MetricApp(metricAppId.Guid, metricAppId.Name));
        }
        dbContext.SaveChanges();

        // Register pre-defined metrics
        foreach (var metricField in typeof(CommonMetrics).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => typeof(MetricKey).IsAssignableFrom(field.FieldType)))
        {
            var metricKey = (MetricKey)metricField.GetValue(null);
            if (dbContext.MetricDefinitions.Any(x => x.MetricGuid == metricKey.Guid)) continue;
            dbContext.MetricDefinitions.Add(new MetricEventDefinition(metricKey.Guid, metricKey.Name));
        }
        dbContext.SaveChanges();

        AppEditorId = dbContext.GetApplicationId(CommonApps.StrideEditorAppId.Guid);
        AppLauncherId = dbContext.GetApplicationId(CommonApps.StrideLauncherAppId.Guid);

        // TODO: comment this for production, only valid for testing the metrics, just run once. Note this is VERY SLOW
        // MetricDbTest.Fill(dbContext);
    }

}
