using System.ComponentModel.DataAnnotations;

namespace Stride.Metrics.ServerApp.Models;

public class IpToLocations
{
    [Key]
    public long IpFrom { get; set; }
    public long IpTo { get; set; }

    [Required, MaxLength(2)]
    public string CountryCode { get; set; }

    [Required, MaxLength(64)]
    public string CountryName { get; set; }
}
