using Stride.Core;
using Stride.Core.Threading;
using Stride.Data;

namespace Stride.BepuPhysics.Navigation.Definitions;
[DataContract("RecastNavigationConfiguration")]
[Display("Recast Navigation")]
public class RecastNavigationConfiguration : Configuration
{
    [Display("Build Settings", Expand = ExpandRule.Never)]
    public BuildSettings BuildSettings { get; set; } = new();

    [Display("Pathfinding Settings", Expand = ExpandRule.Never)]
    public PathfindingSettings PathfindingSettings { get; set; } = new();

    /// <summary>
    /// Total thread count to use for pathfinding. Divided by 2 due to noticable stutter if all threads are used.
    /// </summary>
    public int UsableThreadCount { get; set; } = Dispatcher.MaxDegreeOfParallelism / 2;
}
