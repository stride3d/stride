namespace Stride.Metrics.ServerApp.Dtos;

public record CrashAggregation(int SessionId, int InstallId, string MetricSent, string Version);
