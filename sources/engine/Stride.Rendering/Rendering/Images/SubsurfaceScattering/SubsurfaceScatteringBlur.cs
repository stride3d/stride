using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.Rendering.Materials;

namespace Stride.Rendering.SubsurfaceScattering
{
    [DataContract("SubsurfaceScatteringBlur")]
    [Display("Subsurface Scattering Blur")]
    public class SubsurfaceScatteringBlur : ImageEffect, IImageEffectRenderer, ISharedRenderer
    {
        /// <summary>
        // Changes the render mode of the post-process for debugging purposes.
        /// </summary>
        public enum RenderMode
        {
            /// <userdoc>
            /// Renders the scene as usual.
            /// </userdoc>
            [Display("Default")]
            Default = 0,

            /// <userdoc>
            /// Shows all scattering objects in white and all other objects in black.
            /// </userdoc>
            [Display("Show scattering objects")]
            ShowScatteringObjects = 1,

            /// <userdoc>
            /// Shows the material index of scattering objects as a color.
            /// </userdoc>
            [Display("Show material index")]
            ShowMaterialIndex = 2,

            /// <userdoc>
            /// Shows the width of the scattering kernel as a brightness value (High values will be wrapped around).
            /// Use this to debug if each material gets its own scattering width and doesn't fluctuate.
            /// </userdoc>
            [Display("Show scattering width")]
            ShowScatteringWidth = 3,
        }

        private const int ColorInputIndex = 0;
        private const int DepthStencilInputIndex = 1;
        private const int MaterialIndexInputIndex = 2;

        public const uint MaxMaterialCount = 256;  // 256 elements because the material index texture is only 8 bits deep.

        private ImageEffectShader blurHShader;
        private ImageEffectShader blurVShader;
        private Graphics.Buffer materialScatteringKernelBuffer;

        // TODO: Add an option to set the material index framebuffer bit depth?

        // Array layout:
        // [scattering width 0][scattering width 1][scattering width 2][...]
        private float[] materialScatteringWidths = new float[MaxMaterialCount]; // Preallocate the scattering width array so we don't have to reallocate at runtime (and it simplifies the code a bit).

        // Array layout:
        // [kernel 0 [sample count]][kernel 1 [sample count]][kernel 2 [sample count]][...]
        private Vector4[] materialScatteringKernels = new Vector4[MaxMaterialCount * SubsurfaceScatteringSettings.SamplesPerScatteringKernel2]; // Preallocate the scattering kernel array so we don't have to reallocate at runtime (and it simplifies the code a bit).

        ~SubsurfaceScatteringBlur()
        {
            materialScatteringKernelBuffer?.Dispose();
            materialScatteringKernelBuffer = null;
        }

        private void SetPermutationParameterForBothShaders<T>(PermutationParameterKey<T> parameter, T value) where T : struct
        {
            blurHShader.Parameters.Set(parameter, value);
            blurVShader.Parameters.Set(parameter, value);
        }

        private void SetValueParameterForBothShaders<T>(ValueParameterKey<T> parameter, T value) where T : struct
        {
            blurHShader.Parameters.Set(parameter, value);
            blurVShader.Parameters.Set(parameter, value);
        }

        private void SetValueParameterForBothShaders<T>(ValueParameterKey<T> parameter, ref T value) where T : struct
        {
            blurHShader.Parameters.Set(parameter, ref value);
            blurVShader.Parameters.Set(parameter, ref value);
        }

        private void SetValueParameterForBothShaders<T>(ValueParameterKey<T> parameter, T[] value) where T : struct
        {
            blurHShader.Parameters.Set(parameter, value);
            blurVShader.Parameters.Set(parameter, value);
        }

        private void SetValueParameterForBothShaders<T>(ObjectParameterKey<T> parameter, T value) where T : class
        {
            blurHShader.Parameters.Set(parameter, value);
            blurVShader.Parameters.Set(parameter, value);
        }

        private void SetInputForBothShaders(int slot, Texture texture)
        {
            blurHShader.SetInput(slot, texture);
            blurVShader.SetInput(slot, texture);
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            blurHShader = ToLoadAndUnload(new ImageEffectShader("SubsurfaceScatteringBlurEffect"));
            blurHShader.Initialize(Context);
            blurHShader.Parameters.Set(SubsurfaceScatteringKeys.BlurHorizontally, true);

            blurVShader = ToLoadAndUnload(new ImageEffectShader("SubsurfaceScatteringBlurEffect"));
            blurVShader.Initialize(Context);
            blurVShader.Parameters.Set(SubsurfaceScatteringKeys.BlurHorizontally, false);
        }

        private Vector2 CalculateProjectionSizeOnPlane(RenderView renderView, float viewSpaceDistance)
        {
            Vector3 centerViewSpace = new Vector3(0.0f, 0.0f, -viewSpaceDistance);  // Negate because we are using right handed projection matrices (-Z points into the screen).
            Vector3 topRightViewSpace = new Vector3(1.0f, 1.0f, -viewSpaceDistance);

            Vector3 centerClipSpace = Vector3.TransformCoordinate(centerViewSpace, renderView.Projection);
            Vector3 topRightClipSpace = Vector3.TransformCoordinate(topRightViewSpace, renderView.Projection);

            Vector2 sphereDimensions = new Vector2(topRightClipSpace.X - centerClipSpace.X, topRightClipSpace.Y - centerClipSpace.Y);
            return sphereDimensions;
        }

        private bool UsesOrthographicProjection(RenderView renderView)
        {
            // TODO: STABILITY: Find a more accurate (and maybe faster?) way of detecting orthographic matrices.
            Vector2 projectedSphereDimensionsOnNearPlane = CalculateProjectionSizeOnPlane(renderView, renderView.NearClipPlane);
            Vector2 projectedSphereDimensionsOnFarPlane = CalculateProjectionSizeOnPlane(renderView, renderView.FarClipPlane);

            Vector2 nearFarPlaneRatio = projectedSphereDimensionsOnFarPlane / projectedSphereDimensionsOnNearPlane;

            return nearFarPlaneRatio.Y > 0.01f ? true : false;
        }

        private void UpdatePermutationParameters(RenderDrawContext context)
        {
            SetPermutationParameterForBothShaders(SubsurfaceScatteringKeys.KernelSizeJittering, JitterKernelSize);
            SetPermutationParameterForBothShaders(SubsurfaceScatteringKeys.FollowSurface, FollowSurface ? 1 : 0);
            SetPermutationParameterForBothShaders(SubsurfaceScatteringKeys.MaxMaterialCount, (int)MaxMaterialCount);
            SetPermutationParameterForBothShaders(SubsurfaceScatteringKeys.OrthographicProjection, UsesOrthographicProjection(context.RenderContext.RenderView));
            SetPermutationParameterForBothShaders(SubsurfaceScatteringKeys.KernelLength, SubsurfaceScatteringSettings.SamplesPerScatteringKernel2);
            SetPermutationParameterForBothShaders(SubsurfaceScatteringKeys.RenderMode, (int)ActiveRenderMode);

            // Update the effects (because we modified the permutation parameters):
            blurHShader.EffectInstance.UpdateEffect(context.GraphicsDevice);
            blurVShader.EffectInstance.UpdateEffect(context.GraphicsDevice);
        }

        private unsafe void UpdateKernelBuffer(RenderDrawContext context)
        {
            int bufferSize = materialScatteringKernels.Length * sizeof(Vector4);

            if (materialScatteringKernelBuffer == null)
            {
                materialScatteringKernelBuffer = Graphics.Buffer.New(context.GraphicsDevice, bufferSize, 0, BufferFlags.ShaderResource, PixelFormat.R32G32B32A32_Float);
            }

            fixed (Vector4* dataPtr = materialScatteringKernels)
            {
                DataBox dataBox = new DataBox((IntPtr)dataPtr, 0, 0);
                ResourceRegion resourceRegion = new ResourceRegion(0, 0, 0, bufferSize, 1, 1);  // TODO: PERFORMANCE: Only upload the actual number of elements active in the buffer?
                context.CommandList.UpdateSubresource(materialScatteringKernelBuffer, 0, dataBox, resourceRegion);
            }

            SetValueParameterForBothShaders(SubsurfaceScatteringBlurShaderKeys.KernelBuffer, materialScatteringKernelBuffer);
        }

        private void UpdateParameters(RenderDrawContext context, RenderView renderView)
        {
            // Compute the camera/view parameters:
            Vector2 viewSpaceDepthReconstructionParameters = CameraKeys.ZProjectionACalculate(renderView.NearClipPlane, renderView.FarClipPlane);
            SetValueParameterForBothShaders(CameraKeys.ZProjection, viewSpaceDepthReconstructionParameters);
            SetValueParameterForBothShaders(CameraKeys.NearClipPlane, renderView.NearClipPlane);
            SetValueParameterForBothShaders(CameraKeys.FarClipPlane, renderView.FarClipPlane);

            // Set the values required for the projected sampling radius calculation:
            Vector2 projectionSizeOnUnitPlaneInClipSpace = CalculateProjectionSizeOnPlane(renderView, 1.0f);
            SetValueParameterForBothShaders(SubsurfaceScatteringBlurShaderKeys.ProjectionSizeOnUnitPlaneInClipSpace, projectionSizeOnUnitPlaneInClipSpace);

            // Other parameters:
            SetValueParameterForBothShaders(SubsurfaceScatteringBlurShaderKeys.ViewProjectionMatrix, ref renderView.ViewProjection);    // This is used for debugging only.

            // Supply the material arrays of this frame to the shaders:
            SetValueParameterForBothShaders(SubsurfaceScatteringBlurShaderKeys.ScatteringWidths, materialScatteringWidths);  // TODO: PERFORMANCE: Only upload the actual number of elements used?
            UpdateKernelBuffer(context);
        }

        public void Draw(RenderDrawContext context, Texture color, Texture materialIndex, Texture depthStencil, Texture output)
        {
            SetInput(ColorInputIndex, color);
            SetInput(DepthStencilInputIndex, depthStencil);
            SetInput(MaterialIndexInputIndex, materialIndex);
            SetOutput(output);
            Draw(context);
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            UpdatePermutationParameters(context);
            UpdateParameters(context, context.RenderContext.RenderView);

            // Get the render target attachments to sample from:
            Texture inputFrameBufferColorAttachment = GetSafeInput(ColorInputIndex);
            Texture inputFrameBufferDepthAttachment = GetSafeInput(DepthStencilInputIndex);
            Texture inputFrameBufferIndexAttachment = GetSafeInput(MaterialIndexInputIndex);

            // Get the output:
            Texture outputFrameBuffer = GetSafeOutput(0);

            // Create a temporary color attachment for texture ping ponging:
            Texture inputFrameBufferColorAttachmentCopy = NewScopedRenderTarget2D(inputFrameBufferColorAttachment.Description); // This texture will be allocated only for the scope of this draw and returned to the pool at the exit of this method.

            // Set inputs which are the same for both shaders:
            SetInputForBothShaders(DepthStencilInputIndex, inputFrameBufferDepthAttachment); // Depth attachment -> Texture1
            SetInputForBothShaders(MaterialIndexInputIndex, inputFrameBufferIndexAttachment); // Index attachment -> Texture2

            // Set the horizontal shader texture inputs & output:
            blurHShader.SetInput(ColorInputIndex, inputFrameBufferColorAttachment); // Color attachment -> Texture0
            blurHShader.SetOutput(inputFrameBufferColorAttachmentCopy);

            // Set the vertical shader texture inputs & output:
            blurVShader.SetInput(ColorInputIndex, inputFrameBufferColorAttachmentCopy); // Color attachment -> Texture0
            blurVShader.SetOutput(outputFrameBuffer);

            for (int i = 0; i < NumberOfPasses; ++i)
            {
                //blurHShader.Parameters.Set(SubsurfaceScatteringBlurShaderKeys.AngularOffset, (float)i / (float)NumberOfPasses * System.Math.PI);
                SetValueParameterForBothShaders(SubsurfaceScatteringBlurShaderKeys.IterationNumber, (float)i);

                // Perform the actual rendering:
                blurHShader.Draw(context, "SubsurfaceScattering_blur_H");
                blurVShader.Draw(context, "SubsurfaceScattering_blur_V");
            }
        }

        public void SetScatteringWidth(uint materialIndex, float width)
        {
            if (materialIndex > MaxMaterialCount)
            {
                throw new Exception("Too many scattering materials present in order to be able to fit them all into a single array! Maximum count is " + MaxMaterialCount + ".");
            }

            materialScatteringWidths[materialIndex] = width;
        }

        public void SetScatteringKernel(uint materialIndex, Vector4[] scatteringKernel)
        {
            if (materialIndex > MaxMaterialCount)
            {
                throw new Exception("Too many scattering materials present in order to be able to fit them all into a single array! Maximum count is " + MaxMaterialCount + ".");
            }

            scatteringKernel.CopyTo(materialScatteringKernels, (int)materialIndex * SubsurfaceScatteringSettings.SamplesPerScatteringKernel2);   // Insert into the global scattering kernel array.
        }

        /// <userdoc>
        /// If active, the the light won't scatter across large depth differences.
        /// The depth falloff can be configured using "Depth falloff strength".
        /// Attention: Enabling this increases the performance hit of the effect.
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(true)]
        [Display("Follow surface")]
        public bool FollowSurface { get; set; } = true;

        /// <userdoc>
        /// Specifies the number of times the blur should be executed.
        /// The higher the number of passes, the smoother the final result (less noise & banding).
        /// </userdoc>
        [DataMember(40)]
        [DefaultValue(1)]
        [DataMemberRange(1, 10, 1, 1, 0)]
        [Display("Number of passes")]
        public int NumberOfPasses { get; set; } = 1;

        /// <userdoc>
        // This reduces the banding artifacts caused by undersampling (visible on closeups) by introducing a bit of noise.
        // This might create a less mathematically correct falloff, since it messes with the sample offsets.
        // But the difference is barely noticeable.
        /// </userdoc>
        [DataMember(50)]
        [DefaultValue(false)]
        [Display("Jitter Kernel Size")]
        public bool JitterKernelSize { get; set; } = false;

        /// <userdoc>
        // Changes the render mode of the post-process for debugging purposes.
        /// </userdoc>
        [DataMember(60)]
        [DefaultValue(RenderMode.Default)]
        [Display("Render mode")]
        public RenderMode ActiveRenderMode { get; set; } = RenderMode.Default;

        /// <inheritdoc/>
        [DataMember(-100), Display(Browsable = false)]
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
