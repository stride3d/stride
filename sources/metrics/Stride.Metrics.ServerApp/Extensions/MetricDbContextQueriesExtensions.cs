using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dapper;
using Microsoft.EntityFrameworkCore;
using Stride.Metrics.ServerApp.Data;
using Stride.Metrics.ServerApp.Dtos;
using Stride.Metrics.ServerApp.Dtos.Agregate;

namespace Stride.Metrics.ServerApp.Extensions;

public static class MetricDbContextQueriesExtensions
{
    public static IEnumerable<AggregationPerMonth> GetHighUsage(this MetricDbContext dbContext, int editorAppId)
    {
        using var connection = dbContext.Database.GetDbConnection();
        var highUsage = connection.Query<AggregationPerMonth>(SQLMetricQuery.HighUsage(editorAppId));
        return highUsage;
    }
    public static IEnumerable<ActivityData> GetActivityData(this MetricDbContext dbContext, int editorAppId)
    {
        using var connection = dbContext.Database.GetDbConnection();
        var activityData = connection.Query<ActivityData>(SQLMetricQuery.ActivityData(editorAppId));
        return activityData;
    }
}