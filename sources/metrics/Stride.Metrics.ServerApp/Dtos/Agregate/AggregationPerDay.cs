namespace Stride.Metrics.ServerApp.Dtos.Agregate;

/// <summary>
/// Aggregate data to store metrics results per day.
/// </summary>
public class AggregationPerDay : AggregateBase
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int Day { get; set; }
}
