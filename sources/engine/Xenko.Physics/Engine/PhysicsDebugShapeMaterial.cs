// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering;
using Xenko.Rendering.Materials;
using Xenko.Rendering.Materials.ComputeColors;

namespace Xenko.Physics
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
            material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));

            material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, intensity);
            material.Passes[0].Parameters.Set(MaterialKeys.EmissiveValue, new Color4(color).ToColorSpace(device.ColorSpace));

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
