using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;

namespace Xenko.Rendering.Skyboxes
{
    public abstract class CubemapRendererBase : IDisposable
    {
        private Texture renderTarget;
        private Texture depthStencil;

        protected readonly int OutputTextureSize;
        protected readonly PixelFormat OutputTextureFormat;

        public readonly CameraComponent Camera;
        public RenderDrawContext DrawContext { get; protected set; }

        public CubemapRendererBase(GraphicsDevice device, int outputSize, PixelFormat outputFormat, bool needDepthStencil)
        {
            OutputTextureSize = outputSize;
            OutputTextureFormat = outputFormat;

            Camera = new CameraComponent
            {
                UseCustomProjectionMatrix = true,
                UseCustomViewMatrix = true,
            };
            Camera.ProjectionMatrix = Matrix.PerspectiveFovRH(MathUtil.DegreesToRadians(90.0f), 1.0f, Camera.NearClipPlane, Camera.FarClipPlane);
            
            // We can't render directly to the texture cube before feature level 10.1, so let's render to a standard render target and copy instead
            renderTarget = Texture.New2D(device, OutputTextureSize, OutputTextureSize, outputFormat, TextureFlags.RenderTarget);
            if (needDepthStencil)
                depthStencil = Texture.New2D(device, OutputTextureSize, OutputTextureSize, PixelFormat.D24_UNorm_S8_UInt, TextureFlags.DepthStencil);
        }

        public virtual void Dispose()
        {
            renderTarget.Dispose();
            depthStencil?.Dispose();
        }

        /// <summary>
        /// Render scene from a given position to a cubemap.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="cubeTexture"></param>
        public void Draw(Vector3 position, Texture cubeTexture)
        {
            DrawContext.CommandList.SetRenderTargetAndViewport(depthStencil, renderTarget);

            for (int face = 0; face < 6; ++face)
            {
                // Place camera
                switch ((CubeMapFace)face)
                {
                    case CubeMapFace.PositiveX:
                        Camera.ViewMatrix = Matrix.LookAtRH(position, position + Vector3.UnitX, Vector3.UnitY);
                        break;
                    case CubeMapFace.NegativeX:
                        Camera.ViewMatrix = Matrix.LookAtRH(position, position - Vector3.UnitX, Vector3.UnitY);
                        break;
                    case CubeMapFace.PositiveY:
                        Camera.ViewMatrix = Matrix.LookAtRH(position, position + Vector3.UnitY, Vector3.UnitZ);
                        break;
                    case CubeMapFace.NegativeY:
                        Camera.ViewMatrix = Matrix.LookAtRH(position, position - Vector3.UnitY, -Vector3.UnitZ);
                        break;
                    case CubeMapFace.PositiveZ:
                        Camera.ViewMatrix = Matrix.LookAtRH(position, position - Vector3.UnitZ, Vector3.UnitY);
                        break;
                    case CubeMapFace.NegativeZ:
                        Camera.ViewMatrix = Matrix.LookAtRH(position, position + Vector3.UnitZ, Vector3.UnitY);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                DrawContext.CommandList.BeginProfile(Color.Red, $"Face {(CubeMapFace)face}");

                DrawImpl();

                // Copy to texture cube
                DrawContext.CommandList.CopyRegion(DrawContext.CommandList.RenderTarget, 0, null, cubeTexture, face);

                DrawContext.CommandList.EndProfile();
            }
        }

        protected abstract void DrawImpl();

        public static Texture GenerateCubemap(CubemapRendererBase cubemapRenderer, Vector3 position)
        {
            using (cubemapRenderer)
            {
                // Create target cube texture
                var cubeTexture = Texture.NewCube(cubemapRenderer.DrawContext.GraphicsDevice, cubemapRenderer.OutputTextureSize, cubemapRenderer.OutputTextureFormat);

                using (cubemapRenderer.DrawContext.PushRenderTargetsAndRestore())
                {
                    // Render specular probe
                    cubemapRenderer.DrawContext.GraphicsContext.CommandList.BeginProfile(Color.Red, "SpecularProbe");

                    cubemapRenderer.Draw(position, cubeTexture);

                    cubemapRenderer.DrawContext.GraphicsContext.CommandList.EndProfile();
                }

                return cubeTexture;
            }
        }
    }
}
