namespace Stride.Metrics.ServerApp.Models
{
    /// <summary>
    /// Aggregate data to store metrics results per month and per version.
    /// </summary>
    public class AggregationPerVersion : AggregateBase
    {
        public string Version { get; set; }
    }
}