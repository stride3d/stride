// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    public static class GizmoShaderMaterial
    {
        public static Material Create(GraphicsDevice device, string shaderName)
        {
            var material = Material.New(device, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeShaderClassColor() { MixinReference = shaderName }),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(new ComputeShaderClassColor() { MixinReference = shaderName })
                }
            });

            return material;
        }
    }
}
