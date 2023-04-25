namespace Stride.Metrics.ServerApp.Models;

public class MetricEventDefinitions
{
    public string MetricName { get; set; }
    public int MetricId { get; set; }
    public MetricEvent Metric { get; set; }
}
