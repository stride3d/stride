using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xenko.Metrics.ServerApp.Models
{
    /// <summary>
    /// Defines a metric
    /// </summary>
    public class MetricEventDefinition : MetricCreatedBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricEventDefinition"/> class.
        /// </summary>
        public MetricEventDefinition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricEventDefinition"/> class.
        /// </summary>
        /// <param name="metricGuid">The metric unique identifier.</param>
        /// <param name="metricName">Name of the metric.</param>
        public MetricEventDefinition(Guid metricGuid, string metricName) : this()
        {
            if (metricName == null) throw new ArgumentNullException("metricName");
            MetricGuid = metricGuid;
            MetricName = metricName;
        }

        /// <summary>
        /// Gets or sets the metric identifier.
        /// </summary>
        /// <value>The metric identifier.</value>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MetricId { get; set; }

        /// <summary>
        /// Gets or sets the metric unique identifier.
        /// </summary>
        /// <value>The metric unique identifier.</value>
        [Required]
        [Index(IsUnique = true)]
        public Guid MetricGuid { get; set; }

        /// <summary>
        /// Gets or sets the name of this metric.
        /// </summary>
        /// <value>The name.</value>
        [Required]
        [MaxLength(128)]
        [Index(IsUnique = true)]
        public string MetricName { get; set; }

        /// <summary>
        /// Gets or sets the description of this metric.
        /// </summary>
        /// <value>The description.</value>
        [MaxLength(512)]
        public string Description { get; set; }
    }
}