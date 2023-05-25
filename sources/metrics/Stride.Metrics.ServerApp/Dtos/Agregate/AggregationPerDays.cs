namespace Stride.Metrics.ServerApp.Dtos.Agregate;

public class AggregationPerDays : AggregateBase
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int Day { get; set; }
}
