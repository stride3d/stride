namespace Stride.Metrics.ServerApp.Dtos.Agregate;

/// <summary>
/// Aggregate data to store metrics results per month.
/// </summary>
public class AggregationPerMonth : AggregateBase
{
    public int Year { get; set; }

    public int Month { get; set; }
}
