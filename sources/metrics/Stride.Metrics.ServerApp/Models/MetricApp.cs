using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xenko.Metrics.ServerApp.Models
{
    /// <summary>
    /// Defines a metric application.
    /// </summary>
    public class MetricApp : MetricCreatedBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricApp"/> class.
        /// </summary>
        public MetricApp()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricApp"/> class.
        /// </summary>
        /// <param name="appGuid">The application unique identifier.</param>
        /// <param name="appName">Name of the application.</param>
        public MetricApp(Guid appGuid, string appName) : this()
        {
            AppGuid = appGuid;
            AppName = appName;
        }

        /// <summary>
        /// The application identifier
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AppId { get; set; }

        /// <summary>
        /// Gets or sets the application unique identifier.
        /// </summary>
        /// <value>The application unique identifier.</value>
        [Required]
        [Index(IsUnique = true)]
        public Guid AppGuid { get; set; }

        /// <summary>
        /// The name of this application.
        /// </summary>
        [Required]
        [MaxLength(128)]
        [Index(IsUnique = true)]
        public string AppName { get; set; }
    }
}