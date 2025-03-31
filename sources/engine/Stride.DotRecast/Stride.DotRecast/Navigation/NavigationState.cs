namespace Stride.DotRecast.Navigation;
public enum NavigationState
{
    /// <summary>
    /// Tells the <see cref="NavigationAgentProcessor"/> a plan needs to be queued. This is used internally to prevent multiple path calculations per frame.
    /// </summary>
    QueuePathPlanning,
    /// <summary>
    /// Tells the <see cref="NavigationAgentProcessor"/> to set a new path at the next available opportunity.
    /// </summary>
    PlanningPath,
    /// <summary>
    /// Tells the <see cref="NavigationAgentProcessor"/> the agent has a path.
    /// </summary>
    PathIsReady,
    /// <summary>
    /// Tells the <see cref="NavigationAgentProcessor"/> the agent does not have a valid path.
    /// </summary>
    PathIsInvalid,
}
