using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stride.Metrics.ServerApp.Models
{
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
        [Key, Column(Order = 1, TypeName = "datetime2")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the application identifier <see cref="MetricApp"/> for this metric.
        /// </summary>
        /// <value>The application identifier.</value>
        [Key, Column(Order = 2)]
        public int AppId { get; set; }

        /// <summary>
        /// Gets or sets the application.
        /// </summary>
        /// <value>The application.</value>
        [ForeignKey("AppId")]
        public virtual MetricApp App { get; set; }

        /// <summary>
        /// Gets or sets the install identifier <see cref="MetricInstall"/>.
        /// </summary>
        /// <value>The install identifier.</value>
        [Key, Column(Order = 3)]
        public int InstallId { get; set; }

        /// <summary>
        /// Gets or sets the install.
        /// </summary>
        /// <value>The install.</value>
        [ForeignKey("InstallId")]
        public virtual MetricInstall Install { get; set; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        /// <value>The session identifier.</value>
        [Key, Column(Order = 4)]
        public int SessionId { get; set; }

        /// <summary>
        /// Gets or sets the metric identifier <see cref="MetricEventDefinition"/>
        /// </summary>
        /// <value>The metric identifier.</value>
        [Key, Column(Order = 5)]
        public int MetricId { get; set; }

        /// <summary>
        /// Gets or sets the metric definition.
        /// </summary>
        /// <value>The metric definition.</value>
        [ForeignKey("MetricId")]
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
}