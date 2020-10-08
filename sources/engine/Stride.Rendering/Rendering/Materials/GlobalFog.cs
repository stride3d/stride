using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Rendering.Materials
{
    [DataContract("MaterialFogFeature")]
    [Display("GlobalFog")]
    public class GlobalFog : MaterialFeature, IMaterialFogFeature
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct FogData
        {
            public Vector4 FogColor;
            public float FogStart;
        }

        internal static bool usingGlobalFog = false;

        private static FogData GlobalFogParameters;

        public static void SetGlobalFog(Color3? color = null, float? density = null, float? fogstart = null)
        {
            if (color.HasValue)
            {
                GlobalFogParameters.FogColor.X = color.Value.R;
                GlobalFogParameters.FogColor.Y = color.Value.G;
                GlobalFogParameters.FogColor.Z = color.Value.B;
            }

            if (density.HasValue)
            {
                GlobalFogParameters.FogColor.W = density.Value;
            }

            if (fogstart.HasValue)
            {
                GlobalFogParameters.FogStart = fogstart.Value;
            }

            usingGlobalFog = true;
        }

        public static void GetGlobalFog(out Color3 color, out float density, out float fogstart)
        {
            color = ((Color4)GlobalFogParameters.FogColor).ToColor3();
            density = GlobalFogParameters.FogColor.W;
            fogstart = GlobalFogParameters.FogStart;
        }

        [DataMember]
        public Color3 FogColor
        {
            get => ((Color4)GlobalFogParameters.FogColor).ToColor3();
            set
            {
                GlobalFogParameters.FogColor.X = value.R;
                GlobalFogParameters.FogColor.Y = value.G;
                GlobalFogParameters.FogColor.Z = value.B;
                usingGlobalFog = true;
            }
        }

        [DataMember]
        public float FogDensity
        {
            get => GlobalFogParameters.FogColor.W;
            set
            {
                GlobalFogParameters.FogColor.W = value;
                usingGlobalFog = true;
            }
        }

        [DataMember]
        public float FogStart
        {
            get => GlobalFogParameters.FogStart;
            set
            {
                GlobalFogParameters.FogStart = value;
                usingGlobalFog = true;
            }
        }

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return other is GlobalFog;
        }

        internal static unsafe void PrepareFogConstantBuffer(RenderContext context)
        {
            // adjust for differences in DirectX and Vulkan
            Vector4 usecolor = GlobalFogParameters.FogColor;
            if (GraphicsDevice.Platform == GraphicsPlatform.Vulkan) usecolor.W = 1f / Math.Max(0.000001f, usecolor.W);
            usecolor.W = -usecolor.W; // flip this here so we don't need to do it in the shader

            foreach (var renderFeature in context.RenderSystem.RenderFeatures)
            {
                if (!(renderFeature is RootEffectRenderFeature))
                    continue;

                var renderView = context.RenderView;
                var logicalKey = ((RootEffectRenderFeature)renderFeature).CreateViewLogicalGroup("GlobalFog");
                var viewFeature = renderView.Features[renderFeature.Index];

                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var resourceGroup = viewLayout.Entries[renderView.Index].Resources;

                    var logicalGroup = viewLayout.GetLogicalGroup(logicalKey);
                    if (logicalGroup.Hash == ObjectId.Empty)
                        continue;

                    var mappedCB = (FogData*)(resourceGroup.ConstantBuffer.Data + logicalGroup.ConstantBufferOffset);
                    mappedCB->FogColor = usecolor;
                    mappedCB->FogStart = GlobalFogParameters.FogStart;
                }
            }
        }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            usingGlobalFog = true;

            var shaderBuilder = context.AddShading(this);
            shaderBuilder.ShaderSources.Add(new ShaderClassSource("FogFeature"));
        }
    }
}
