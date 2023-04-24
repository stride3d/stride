using System.ComponentModel.DataAnnotations;
using Stride.Metrics.ServerApp.Models.MetricCreated;

namespace Stride.Metrics.ServerApp.Models;

/// <summary>
/// A metric event that measure a value for a particular application, installation and session.
/// </summary>
public class MetricEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricEvent"/> class.
    /// </summary>
    public MetricEvent()
    {
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets or sets the timestamp the metric was issued.
    /// </summary>
    /// <value>The timestamp.</value>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the application identifier <see cref="MetricApp"/> for this metric.
    /// </summary>
    /// <value>The application identifier.</value>
    public int AppId { get; set; }

    /// <summary>
    /// Gets or sets the application.
    /// </summary>
    /// <value>The application.</value>
    public virtual MetricApp App { get; set; }

    /// <summary>
    /// Gets or sets the install identifier <see cref="MetricInstall"/>.
    /// </summary>
    /// <value>The install identifier.</value>
    public int InstallId { get; set; }

    /// <summary>
    /// Gets or sets the install.
    /// </summary>
    /// <value>The install.</value>
    public virtual MetricInstall Install { get; set; }

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    /// <value>The session identifier.</value>
    public int SessionId { get; set; }

    /// <summary>
    /// Gets or sets the metric identifier <see cref="MetricEventDefinition"/>
    /// </summary>
    /// <value>The metric identifier.</value>
    public int MetricId { get; set; }

    /// <summary>
    /// Gets or sets the metric definition.
    /// </summary>
    /// <value>The metric definition.</value>
    public virtual MetricEventDefinition MetricEventDefinition { get; set; }

    /// <summary>
    /// Gets or sets the ip address.
    /// </summary>
    /// <value>The ip address.</value>
    [MaxLength(20)]
    public string IPAddress { get; set; }

    /// <summary>
    /// Gets or sets the metric value.
    /// </summary>
    /// <value>The metric value.</value>
    [Required(AllowEmptyStrings = true)]
    [MaxLength(NewMetricMessage.MaxValueLength)]
    public string MetricValue { get; set; }

    /// <summary>
    /// Progressive count of the current session's events
    /// </summary>
    public int EventId { get; set; }

    internal bool Validate()
    {
        return AppId > 0 && InstallId > 0 && SessionId >= 0 && MetricId > 0 && MetricValue != null;
    }
}
