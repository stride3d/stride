// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Physics
{
    public static class PhysicsDebugShapeMaterial
    {
        public static Material CreateDefault(GraphicsDevice device, Color color, float intensity)
        {
            return CreateInternal(device, color, intensity, false);
        }

        //public static Material CreateStaticPlane(GraphicsDevice device, Color color, float intensity)
        //{
        //    return CreateInternal(device, color, intensity, true);
        //}

        private static Material CreateInternal(GraphicsDevice device, Color color, float intensity, bool recenterMesh)
        {
            IComputeColor diffuseMap = new ComputeColor();

            // TODO: Implement the shader and enable this
            //if (recenterMesh)
            //{
            //    var meshRecenterEffect = new ComputeShaderClassColor { MixinReference = "PhysicsStaticPlaneDebugDiffuse" };
            //    diffuseMap = new ComputeBinaryColor(new ComputeColor(), meshRecenterEffect, BinaryOperator.Multiply);
            //}

            var material = Material.New(device, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(diffuseMap),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor()),
                },
            });

            // set the color to the material
            var materialColor = new Color4(color).ToColorSpace(device.ColorSpace);
            material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, ref materialColor);

            material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, intensity);
            material.Passes[0].Parameters.Set(MaterialKeys.EmissiveValue, ref materialColor);

            return material;
        }

        public static Material CreateHeightfieldMaterial(GraphicsDevice device, Color color, float intensity)
        {
            var colorVertexStream = new ComputeVertexStreamColor { Stream = new ColorVertexStreamDefinition() };
            var computeColor = new ComputeBinaryColor(new ComputeColor(new Color4(color).ToColorSpace(device.ColorSpace)), colorVertexStream, BinaryOperator.Multiply);

            var material = Material.New(device, new MaterialDescriptor
            {
                Attributes = new MaterialAttributes
                {
                    Diffuse = new MaterialDiffuseMapFeature(computeColor),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(computeColor),
                },
            });

            // set the color to the material
            material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, intensity);

            return material;
        }
    }
}
