using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.BepuPhysics.Navigation;
public enum NavigationState
{
    /// <summary>
    /// Tells the <see cref="RecastNavigationProcessor"/> a plan needs to be queued. This is used internally to prevent multiple path calculations per frame.
    /// </summary>
    QueuePathPlanning,
    /// <summary>
    /// Tells the <see cref="RecastNavigationProcessor"/> to set a new path at the next available opportunity.
    /// </summary>
    PlanningPath,
    /// <summary>
    /// Tells the <see cref="RecastNavigationProcessor"/> the agent has a path.
    /// </summary>
    PathIsReady,
    /// <summary>
    /// Tells the <see cref="RecastNavigationProcessor"/> the agent does not have a valid path.
    /// </summary>
    PathIsInvalid,
}
