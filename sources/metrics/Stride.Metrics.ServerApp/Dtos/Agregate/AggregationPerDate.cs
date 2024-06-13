namespace Stride.Metrics.ServerApp.Dtos.Agregate;

/// <summary>
/// Aggregate data to store metrics results per day.
/// </summary>
public class AggregationPerDate : AggregateBase
{
    public DateTime Date { get; set; }
}
