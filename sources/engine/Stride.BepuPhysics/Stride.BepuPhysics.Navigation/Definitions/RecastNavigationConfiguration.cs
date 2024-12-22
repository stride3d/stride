using Stride.Core;
using Stride.Core.Threading;
using Stride.Data;
using Stride.DotRecast.Definitions;

namespace Stride.BepuPhysics.Navigation.Definitions;
[DataContract("RecastNavigationConfiguration")]
[Display("Recast Navigation")]
public class RecastNavigationConfiguration : Configuration
{
    [Display("Pathfinding Settings", Expand = ExpandRule.Never)]
    public PathfindingSettings PathfindingSettings { get; set; } = new();

    [Display("NavMeshes", Expand = ExpandRule.Once)]
    public List<BepuNavMeshInfo> NavMeshes = [];

    /// <summary>
    /// Total thread count to use for pathfinding. Divided by 2 due to noticable stutter if all threads are used.
    /// </summary>
    public int UsableThreadCount { get; set; } = Dispatcher.MaxDegreeOfParallelism / 2;
}
