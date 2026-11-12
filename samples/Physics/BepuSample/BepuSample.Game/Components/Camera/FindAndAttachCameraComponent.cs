using System.Threading.Tasks;
using BepuSample.Game.Extensions;
using Stride.Engine;
using Stride.Core.Mathematics;

namespace BepuSample.Game.Components.Camera;
public class FindAndAttachCameraComponent : AsyncScript
{
    public CameraComponent CameraComponent { get; set; }

    public FindAndAttachCameraComponent()
    {
        // operate as a post process to other game logic to ensure
        // we have the latest transform data to work off of
        this.Priority = 1;
    }

    public override async Task Execute()
    {
        CameraComponent ??= SetMainSceneCamera(SceneSystem.SceneInstance);
        if (CameraComponent is null)
            return;

        var initialPosition = CameraComponent.Entity.Transform.Position;
        var initialRotation = CameraComponent.Entity.Transform.Rotation;

        // Interpolate between start and target
        for (float t = 0; t < 1f; t += (float)Game.UpdateTime.Elapsed.TotalSeconds)
        {
            Entity.Transform.UpdateWorldMatrix();
            Entity.Transform.GetWorldTransformation(out var targetPosition, out var targetRotation, out _);

            var newPosition = Vector3.Lerp(initialPosition, targetPosition, t);
            CameraComponent.Entity.Transform.Position = newPosition;
            Quaternion.Slerp(ref initialRotation, ref targetRotation, t, out var newRotation);
            CameraComponent.Entity.Transform.Rotation = newRotation;
            await Script.NextFrame();
        }

        while (this.CancellationToken.IsCancellationRequested == false)
        {
            Entity.Transform.UpdateWorldMatrix();
            Entity.Transform.GetWorldTransformation(out CameraComponent.Entity.Transform.Position, out CameraComponent.Entity.Transform.Rotation, out _);
            await Script.NextFrame();
        }
    }

    private CameraComponent? SetMainSceneCamera(SceneInstance sceneInstance)
    {
        CameraComponent? camera = null;

        foreach (var entity in sceneInstance)
        {
            camera = entity.GetComponentInChildren<CameraComponent>();

            if (camera != null)
            {
                break;
            }
        }

        return camera;
    }
}
