using Stride.BepuPhysics.Demo.Extensions;
using Stride.Engine;

namespace Stride.BepuPhysics.Demo.Components.Camera;
public class FindAndAttachCameraComponent : SyncScript
{

    public CameraComponent CameraComponent { get; set; }

    public override void Start()
    {
        CameraComponent ??= SetMainSceneCamera(SceneSystem.SceneInstance);
    }

    public override void Update()
    {
        CameraComponent.Entity.Transform.Position = Entity.Transform.Position;
        CameraComponent.Entity.Transform.Rotation = Entity.Transform.Rotation;
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
