using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xenko.Metrics.ServerApp.Models
{
    /// <summary>
    /// A marker that defines an important event on a specific date that could have an impact on metric events.
    /// </summary>
    public class MetricMarker : MetricCreatedBase
    {
        /// <summary>
        /// Gets or sets the metric marker identifier.
        /// </summary>
        /// <value>The metric marker identifier.</value>
        [Key]
        public int MarkerId { get; set; }

        /// <summary>
        /// Gets or sets the metric marker group identifier that identifies the group of marker this marker belongs to.
        /// </summary>
        /// <value>The metric marker group identifier.</value>
        public int MarkerGroupId { get; set; }

        /// <summary>
        /// Gets or sets the marker group.
        /// </summary>
        /// <value>The marker group.</value>
        [ForeignKey("MarkerGroupId")]
        public virtual MetricMarkerGroup MarkerGroup { get; set; }

        /// <summary>
        /// Gets or sets the name that describe this event.
        /// </summary>
        /// <value>The name.</value>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the date this marker is bound to.
        /// </summary>
        /// <value>The date.</value>
        public DateTime Date { get; set; }
    }
}