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

    public DbSet<MetricEvent> MetricEvents { get; set; }

    public DbSet<MetricApp> MetricApps { get; set; }

    public DbSet<MetricEventDefinition> MetricEventDefinitions { get; set; }

    public DbSet<MetricInstall> MetricInstalls { get; set; }

    public DbSet<MetricMarker> MetricMarkers { get; set; }

    public DbSet<MetricMarkerGroup> MetricMarkerGroups { get; set; }
    
    public DbSet<IpToLocations> IpToLocations { get; set; }

    public static int AppEditorId { get; set; }

    public static int AppLauncherId { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MetricApp>()
            .HasIndex(a => a.AppName)
            .IsUnique();

        modelBuilder.Entity<MetricApp>()
            .HasIndex(a => a.AppGuid)
            .IsUnique();

        modelBuilder.Entity<MetricEventDefinition>()
           .HasIndex(a => a.MetricGuid)
           .IsUnique();

        modelBuilder.Entity<MetricEventDefinition>()
           .HasIndex(a => a.MetricName)
           .IsUnique();

        modelBuilder.Entity<MetricInstall>()
           .HasIndex(a => a.InstallGuid)
           .IsUnique();

        modelBuilder.Entity<MetricEvent>()
            .HasKey(m => new { m.Timestamp, m.AppId, m.InstallId, m.SessionId, m.MetricId });

        modelBuilder.Entity<MetricEvent>()
            .Property(m => m.Timestamp)
            .HasColumnType("datetime2");

        modelBuilder.Entity<IpToLocations>()
            .HasIndex(a => a.IpFrom)
            .IsUnique();

        modelBuilder.Entity<IpToLocations>()
            .HasIndex(a => a.IpTo)
            .IsUnique();

        modelBuilder.HasDbFunction(
            typeof(MetricDbContext).GetMethod(nameof(IPAddressToCountry), 
            new[] { typeof(string) }))
            .HasName("IPAddressToCountry");
    }

    /// <summary>
    ///  SQL Function representation, if is not available will throw NotImplementedException
    /// </summary>
    /// <param name="IPAddress"></param>
    /// <returns></returns>
    public string IPAddressToCountry(string IPAddress)
    {
        throw new NotImplementedException();
    }
}
