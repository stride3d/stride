using System.ComponentModel.DataAnnotations;

namespace Xenko.Metrics.ServerApp.Models
{
    /// <summary>
    /// A group of markers.
    /// </summary>
    public class MetricMarkerGroup : MetricCreatedBase
    {
        /// <summary>
        /// Gets or sets the metric marker group identifier.
        /// </summary>
        /// <value>The metric marker group identifier.</value>
        [Key]
        public int MarkerGroupId { get; set; }

        /// <summary>
        /// Gets or sets the name that describe this event.
        /// </summary>
        /// <value>The name.</value>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }
    }
}