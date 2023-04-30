using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stride.Metrics.ServerApp.Models;
using Stride.Metrics.ServerApp.Models.MetricCreated;

namespace Stride.Metrics.ServerApp.Data;

public static class MetricDbContextExtensions
{
    private static readonly Dictionary<Guid, int> AppIds = new();
    private static readonly Dictionary<Guid, int> InstallIds = new();
    private static readonly Dictionary<Guid, int> MetricIds = new();

    public static async Task<List<T>> SqlToList<T>(this MetricDbContext dbContext, string sqlQuery, params object[] parameters)
    {
        return await dbContext.Database.SqlQueryRaw<T>(sqlQuery, parameters).ToListAsync();
    }

    public static int GetApplicationId(this MetricDbContext context, Guid appGuid)
    {
        int id;
        lock (AppIds)
        {
            if (!AppIds.TryGetValue(appGuid, out id))
            {
                id = -1;
                var app = context.Apps.FirstOrDefault(t => t.AppGuid == appGuid);
                if (app != null)
                {
                    id = app.AppId;
                    AppIds.Add(appGuid, id);
                }
            }
        }
        return id;
    }

    public static int GetOrCreateInstallId(this MetricDbContext context, Guid installGuid)
    {
        int id;
        lock (InstallIds)
        {
            if (!InstallIds.TryGetValue(installGuid, out id))
            {
                // Create automatically install id on first encounter
                var install = context.Installs.FirstOrDefault(t => t.InstallGuid == installGuid);

                if (install == null)
                {
                    install = new MetricInstall(installGuid);
                    context.Installs.Add(install);
                    context.SaveChanges();
                }

                id = install.InstallId;
                InstallIds.Add(installGuid, id);
            }
        }
        return id;
    }

    public static int GetMetricId(this MetricDbContext context, Guid metricGuid)
    {
        int id;
        lock (MetricIds)
        {
            if (!MetricIds.TryGetValue(metricGuid, out id))
            {
                id = -1;
                var metricDef = context.MetricDefinitions.FirstOrDefault(t => t.MetricGuid == metricGuid);
                if (metricDef != null)
                {
                    id = metricDef.MetricId;
                    MetricIds.Add(metricGuid, id);
                }
            }
        }
        return id;
    }
    internal static MetricEvent SaveNewMetric(this MetricDbContext dbContext, NewMetricMessage newMetric, string ipAddress, DateTime? overrideTime = null, bool disableSave = false)
    {
        var applicationId = dbContext.GetApplicationId(newMetric.ApplicationId);
        if (applicationId == -1)
        {
            Trace.TraceError($"Invalid ApplicationId {newMetric.ApplicationId} from {ipAddress}");
            throw new InvalidOperationException($"Invalid ApplicationId {newMetric.ApplicationId} from {ipAddress}");
        }

        var installId = dbContext.GetOrCreateInstallId(newMetric.InstallId);
        var metricId = dbContext.GetMetricId(newMetric.MetricId);

        var metricEvent = new MetricEvent
        {
            AppId = applicationId,
            InstallId = installId,
            SessionId = newMetric.SessionId,
            MetricId = metricId,
            IPAddress = ipAddress,
            EventId = newMetric.EventId,
            MetricValue = newMetric.Value ?? string.Empty,
            Timestamp = overrideTime ?? default
        };

        var metricAsJson = JsonConvert.SerializeObject(newMetric);
        if (!metricEvent.Validate())
        {
            Trace.TraceError($"Invalid MetricEvent, cannot save: {metricAsJson}");
            throw new InvalidOperationException($"Invalid MetricEvent {metricAsJson}");
        }

        // If element valid, use the MetricEvent for loggin from here
        metricAsJson = JsonConvert.SerializeObject(metricEvent);

        var newMetricEvent = dbContext.Metrics.Add(metricEvent);
        if (!disableSave)
        {
            try
            {
                dbContext.SaveChanges();
                return newMetricEvent.Entity;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unable to save MetricEvent, cannot save: {0} {1}", ex, metricAsJson);
                throw;
            }
        }

        return null;
    }
}
