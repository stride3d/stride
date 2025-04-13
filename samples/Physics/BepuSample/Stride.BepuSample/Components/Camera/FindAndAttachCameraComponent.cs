using System;
using Stride.BepuSample.Extensions;
using Stride.Engine;
using Stride.Core.Mathematics;

namespace Stride.BepuSample.Components.Camera;
public class FindAndAttachCameraComponent : SyncScript
{

    public CameraComponent CameraComponent { get; set; }

    public override void Start()
    {
        CameraComponent ??= SetMainSceneCamera(SceneSystem.SceneInstance);
    }

    public override void Update()
    {
        var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        var currentPosition = CameraComponent.Entity.Transform.Position;
        var currentRotation = CameraComponent.Entity.Transform.Rotation;

        var lerpSpeed = 1f - MathF.Exp(-100 * deltaTime);

        Entity.Transform.GetWorldTransformation(out var otherPosition, out var otherRotation, out var _);

        var newPosition = Vector3.Lerp(currentPosition, otherPosition, lerpSpeed);
        CameraComponent.Entity.Transform.Position = newPosition;
        Quaternion.Slerp(ref currentRotation, ref otherRotation, lerpSpeed, out var newRotation);
        CameraComponent.Entity.Transform.Rotation = newRotation;
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
