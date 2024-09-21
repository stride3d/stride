// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    public static class GizmoUniformColorMaterial
    {
        public static Material Create(GraphicsDevice device, Color color)
        {
            var desc = new MaterialDescriptor();
            desc.Attributes.Diffuse = new MaterialDiffuseMapFeature(new ComputeColor());
            desc.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();

            var material = Material.New(device, desc);

            // set the color to the material
            UpdateColor(device, material, color);

            // set the transparency property to the material if necessary
            if (color.A < Byte.MaxValue)
            {

                material.Passes[0].HasTransparency = true;
                // TODO GRAPHICS REFACTOR
                //material.Parameters.SetResourceSlow(Graphics.Effect.BlendStateKey, device.BlendStates.NonPremultiplied);
            }

            return material;
        }

        public static void UpdateColor(GraphicsDevice device, Material material, Color color)
        {
            // set the color to the material
            material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));
        }
    }
}
