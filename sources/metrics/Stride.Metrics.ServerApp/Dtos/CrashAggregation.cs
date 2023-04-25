namespace Stride.Metrics.ServerApp.Dtos;

public record CrashAggregation(int VersionId, int SessionId, string MetricSent, string Version, int Appid);
