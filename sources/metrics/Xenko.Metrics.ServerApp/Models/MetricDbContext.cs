using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Xenko.Metrics.ServerApp.Controllers;
using Xenko.Metrics.ServerApp.Migrations;

namespace Xenko.Metrics.ServerApp.Models
{
    public class MetricDbContext : DbContext
    {
        public MetricDbContext()
            : this("DefaultConnection")
        {
        }

        public MetricDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            // Get the ObjectContext related to this DbContext
            var objectContext = (this as IObjectContextAdapter).ObjectContext;

            // Sets the command timeout for all the commands
            objectContext.CommandTimeout = 240;
        }

        public DbSet<MetricEvent> Metrics { get; set; }

        public DbSet<MetricApp> Apps { get; set; }

        public DbSet<MetricEventDefinition> MetricDefinitions { get; set; }

        public DbSet<MetricInstall> Installs { get; set; }

        public DbSet<MetricMarker> Markers { get; set; }

        public DbSet<MetricMarkerGroup> MarkerGroups { get; set; }

        public static int AppEditorId { get; private set; }

        public static int AppLauncherId { get; private set; }

        internal MetricEvent SaveNewMetric(NewMetricMessage newMetric, string ipAddress, DateTime? overrideTime = null, bool disableSave = false)
        {
            var applicationId = this.GetApplicationId(newMetric.ApplicationId);
            if (applicationId == -1)
            {
                Trace.TraceError($"Invalid ApplicationId {newMetric.ApplicationId} from {ipAddress}");
                throw new InvalidOperationException($"Invalid ApplicationId {newMetric.ApplicationId} from {ipAddress}");
            }

            //Debug.WriteLine("Save - ApplicationId {0}ms", clock.ElapsedMilliseconds);
            //clock.Restart();

            var installId = this.GetOrCreateInstallId(newMetric.InstallId);
            //Debug.WriteLine("Save - InstallId {0}ms", clock.ElapsedMilliseconds);
            //clock.Restart();

            var metricId = this.GetMetricId(newMetric.MetricId);
            //Debug.WriteLine("Save - MetricId {0}ms", clock.ElapsedMilliseconds);
            //clock.Restart();

            var metricEvent = new MetricEvent
            {
                AppId = applicationId,
                InstallId = installId,
                SessionId = newMetric.SessionId,
                MetricId = metricId,
                IPAddress = ipAddress,
                EventId = newMetric.EventId,
                MetricValue = newMetric.Value ?? string.Empty
            };

            if (overrideTime.HasValue)
            {
                metricEvent.Timestamp = overrideTime.Value;
            }

            var metricAsJson = JsonConvert.SerializeObject(newMetric);
            if (!metricEvent.Validate())
            {
                Trace.TraceError($"Invalid MetricEvent, cannot save: {metricAsJson}");
                throw new InvalidOperationException($"Invalid MetricEvent {metricAsJson}");
            }

            // If element valid, use the MetricEvent for loggin from here
            metricAsJson = JsonConvert.SerializeObject(metricEvent);

            var newMetricEvent = Metrics.Add(metricEvent);
            if (!disableSave)
            {
                try
                {
                    SaveChanges();
                    return newMetricEvent;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Unable to save MetricEvent, cannot save: {0} {1}", ex, metricAsJson);
                    throw;
                }
            }

            return null;
        }

        internal static void Initialize()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MetricDbContext, Configuration>()); 

            using (var db = new MetricDbContext())
            {
                // Register pre-defined applications
                foreach (
                        var metricAppField in
                            typeof(CommonApps).GetFields()
                                .Where(
                                    field =>
                                        field.IsStatic &&
                                        typeof(MetricAppId).IsAssignableFrom(field.FieldType)))
                {
                    var metricAppId = (MetricAppId)metricAppField.GetValue(null);
                    if (db.Apps.Any(x => x.AppGuid == metricAppId.Guid)) continue;
                    db.Apps.Add(new MetricApp(metricAppId.Guid, metricAppId.Name));
                }
                db.SaveChanges();

                // Register pre-defined metrics
                foreach (
                        var metricField in
                            typeof(CommonMetrics).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                .Where(
                                    field =>
                                        typeof(MetricKey).IsAssignableFrom(field.FieldType)))
                {
                    var metricKey = (MetricKey) metricField.GetValue(null);
                    if (db.MetricDefinitions.Any(x => x.MetricGuid == metricKey.Guid)) continue;
                    db.MetricDefinitions.Add(new MetricEventDefinition(metricKey.Guid, metricKey.Name));
                }
                db.SaveChanges();

                AppEditorId = db.GetApplicationId(CommonApps.XenkoEditorAppId.Guid);
                AppLauncherId = db.GetApplicationId(CommonApps.XenkoLauncherAppId.Guid);

                // TODO: comment this for production, only valid for testing the metrics, just run once. Note this is VERY SLOW
                // MetricDbTest.Fill(db);
            }
        }
    }
}
