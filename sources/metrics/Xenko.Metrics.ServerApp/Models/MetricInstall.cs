using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xenko.Metrics.ServerApp.Models
{
    /// <summary>
    /// Defines an installation of the metric application.
    /// </summary>
    public class MetricInstall : MetricCreatedBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricInstall"/> class.
        /// </summary>
        public MetricInstall()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricInstall"/> class.
        /// </summary>
        /// <param name="installGuid">The install unique identifier.</param>
        public MetricInstall(Guid installGuid) : this()
        {
            InstallGuid = installGuid;
        }

        /// <summary>
        /// Gets or sets the install identifier.
        /// </summary>
        /// <value>The install identifier.</value>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InstallId { get; set; }
        
        /// <summary>
        /// Gets or sets the install unique identifier.
        /// </summary>
        /// <value>The install unique identifier.</value>
        [Required]
        [Index(IsUnique = true)]
        public Guid InstallGuid { get; set; }
    }
}