using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Core.Annotations;

namespace Stride.BepuPhysics.Navigation.Components;
[DataContract(nameof(RecastPhysicsNavigationComponent))]
[ComponentCategory("Bepu - Navigation")]
public class RecastPhysicsNavigationComponent : RecastNavigationComponent
{

    [MemberRequired]
    public required CharacterComponent PhysicsComponent { get; set; }

    public override void StartFollowingPath()
    {
        IsMoving = true;
    }

    public override void StopFollowingPath()
    {
        IsMoving = false;
        PhysicsComponent.Move(Vector3.Zero);
    }

    public override void Update(float deltaTime)
    {
        if (IsMoving)
        {
            Move();
            Rotate();
        }
    }

    private void Move()
    {
        if (Path.Count == 0)
        {
            return;
        }

        var position = Entity.Transform.WorldMatrix.TranslationVector;

        var nextWaypointPosition = Path[0];
        var distanceToWaypoint = Vector3.Distance(position, nextWaypointPosition);

        // When the distance between the character and the next waypoint is large enough, move closer to the waypoint
        if (distanceToWaypoint > 0.5)
        {
            var direction = nextWaypointPosition - position;
            direction.Normalize();

            PhysicsComponent.Move(direction);
        }
        else
        {
            if (Path.Count > 0)
            {
                // need to test if storing the index in Pathfinder would be faster than this.
                Path.RemoveAt(0);
            }
        }
    }

    private void Rotate()
    {
        if (Path.Count == 0)
        {
            return;
        }
        var position = Entity.Transform.WorldMatrix.TranslationVector;

        float angle = (float)Math.Atan2(Path[0].Z - position.Z,
            Path[0].X - position.X);

        Entity.Transform.Rotation = Quaternion.RotationY(-angle);
        PhysicsComponent.Orientation = Entity.Transform.Rotation;
    }
}
