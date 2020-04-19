using Stride.Core.Annotations;
using Stride.Engine;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// A camera renderer that can use an external camera not in the scene.
    /// </summary>
    [NonInstantiable]
    public class SceneExternalCameraRenderer : SceneCameraRenderer
    {
        public CameraComponent ExternalCamera { get; set; }

        /// <summary>
        /// Resolves camera to <see cref="ExternalCamera"/> rather than the default behavior.
        /// </summary>
        protected override CameraComponent ResolveCamera(RenderContext renderContext)
        {
            return ExternalCamera;
        }
    }
}