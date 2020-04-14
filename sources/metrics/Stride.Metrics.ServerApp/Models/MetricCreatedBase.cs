using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xenko.Metrics.ServerApp.Models
{
    /// <summary>
    /// A base class that provides a automatically <see cref="Created"/> field
    /// </summary>
    public abstract class MetricCreatedBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricInstall"/> class.
        /// </summary>
        protected MetricCreatedBase()
        {
            Created = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets or sets when this instance was created.
        /// </summary>
        /// <value>The datetime when this instance was created.</value>
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime Created { get; set; }
    }
}