using Stride.BepuPhysics.Navigation.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Navigation.Components;
[DataContract(nameof(RecastNavigationComponent))]
[ComponentCategory("Bepu - Navigation")]
[DefaultEntityComponentProcessor(typeof(RecastNavigationProcessor), ExecutionMode = ExecutionMode.Runtime)]
public class RecastNavigationComponent : StartupScript
{
    /// <summary>
    /// The speed at which the agent moves.
    /// </summary>
    public float Speed { get; set; } = 5.0f;

    /// <summary>
    /// The current state of the agent.
    /// </summary>
    [DataMemberIgnore]
    public NavigationState State { get; set; }

    /// <summary>
    /// The target position for the agent to move to.
    /// </summary>
    [DataMemberIgnore]
    public Vector3 Target { get; protected set; }

    /// <summary>
    /// The calculated path for the agent to follow.
    /// </summary>
    [DataMemberIgnore]
    public List<Vector3> Path = new();

    /// <summary>
    /// The list of polygons that make up the path.
    /// </summary>
    [DataMemberIgnore]
    public List<long> Polys = new();

    /// <summary>
    /// Is true when the agent is trying to follow an existing path. This can be set to false if the path is no longer valid.
    /// </summary>
    public bool IsMoving { get; protected set; }

    private RecastNavigationProcessor _navigationProcessor;

    /// <summary>
    /// Sets the target for the agent to find a path to. This will set the <see cref="State"/> to <see cref="NavigationState.QueuePathPlanning"/>.
    /// </summary>
    /// <param name="target"></param>
    public virtual void SetTarget(Vector3 target)
    {
        Target = target;
        State = NavigationState.QueuePathPlanning;
    }

    /// <summary>
    /// Tries to find a path to the target. This will set the <see cref="State"/> to <see cref="NavigationState.PathIsReady"/> if a path is found.
    /// <para>
    /// To use the built in multithreaded queue call <see cref="SetTarget(Vector3)"/> to set the target instead.
    /// </para>
    /// </summary>
    /// <returns></returns>
    public virtual bool TryFindPath(Vector3 target)
    {
        _navigationProcessor ??= Services.GetSafeServiceAs<RecastNavigationProcessor>();

        Target = target;
        if (_navigationProcessor.SetNewPath(this))
        {
            State = NavigationState.PathIsReady;
            return true;
        }
        State = NavigationState.PathIsInvalid;
        return false;
    }

    /// <summary>
    /// Once the path has been calculated, the agent will start moving towards the target. Call <see cref="SetTarget(Vector3)"/> to find a valid path.
    /// </summary>
    public virtual void StartFollowingPath()
    {
        IsMoving = true;
    }

    /// <summary>
    /// Stops the agent from trying to move.
    /// </summary>
    public virtual void StopFollowingPath()
    {
        IsMoving = false;
    }

    /// <summary>
    /// Controls what the agent should do per frame. By default, this will move the agent towards the target if <see cref="IsMoving"/> is true.
    /// <para>This method is run in multiple threads so data safety is crucial.</para>
    /// </summary>
    /// <param name="deltaTime"></param>
    public virtual void Update(float deltaTime)
    {
        // This allows the agent to move towards the target even if a path is being planned.
        // This allows  the user to determine if the agent should stop moving if the path is no longer valid.
        if (IsMoving)
        {
            MoveAndRotate(deltaTime);
        }
    }

    private void MoveAndRotate(float deltaTime)
    {
        if (Path.Count == 0)
        {
            State = NavigationState.PathIsInvalid;
            return;
        }

        var worldPosition = Entity.Transform.WorldMatrix.TranslationVector;

        var nextWaypointPosition = Path[0];
        var rotation = Quaternion.LookRotation(Vector3.Normalize(Path[0] - worldPosition), Vector3.UnitY);
        var targetPosition = Vector3.MoveTo(worldPosition, nextWaypointPosition, Speed * deltaTime);
        var scale = Entity.Transform.Scale;

        if (targetPosition == nextWaypointPosition && Path.Count > 0)
        {
            // need to test if storing the index in Pathfinder would be faster than this.
            Path.RemoveAt(0);
        }

        // Handle the scenario where the agent has a parent.
        Entity.Transform.Parent?.WorldToLocal(ref targetPosition, ref rotation, ref scale);

        Entity.Transform.Position = targetPosition;
        Entity.Transform.Rotation = rotation;// Quaternion.Lerp(Entity.Transform.Rotation, rotation, .1f);
    }
}
