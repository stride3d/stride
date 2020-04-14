namespace Stride.Metrics.ServerApp.Models
{
    /// <summary>
    /// Aggregate data to store metrics results per month.
    /// </summary>
    public class AggregationPerMonth : AggregateBase
    {
        public int Year { get; set; }

        public int Month { get; set; }
    }

    public class AggregationPerDays: AggregateBase
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }
    }
}