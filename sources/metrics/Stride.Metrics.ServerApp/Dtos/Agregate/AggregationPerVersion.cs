namespace Stride.Metrics.ServerApp.Dtos.Agregate;

/// <summary>
/// Aggregate data to store metrics results per month and per version.
/// </summary>
public class AggregationPerVersion : AggregateBase
{
    public string Version { get; set; }
}