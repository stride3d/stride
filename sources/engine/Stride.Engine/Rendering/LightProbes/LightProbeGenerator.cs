// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect.LambertianPrefiltering;
using Stride.Rendering.Images.SphericalHarmonics;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Skyboxes;

namespace Stride.Rendering.LightProbes
{
    public static class LightProbeGenerator
    {
        public const int LambertHamonicOrder = 3;

        public static Dictionary<LightProbeComponent, FastList<Color3>> GenerateCoefficients(ISceneRendererContext context)
        {
            using (var cubemapRenderer = new CubemapSceneRenderer(context, 256))
            {
                // Create target cube texture
                var cubeTexture = Texture.NewCube(context.GraphicsDevice, 256, PixelFormat.R16G16B16A16_Float);

                // Prepare shader for SH prefiltering
                var lambertFiltering = new LambertianPrefilteringSHNoCompute(cubemapRenderer.DrawContext.RenderContext)
                {
                    HarmonicOrder = LambertHamonicOrder,
                    RadianceMap = cubeTexture,
                };

                var lightProbesCoefficients = new Dictionary<LightProbeComponent, FastList<Color3>>();

                using (cubemapRenderer.DrawContext.PushRenderTargetsAndRestore())
                {
                    // Render light probe
                    context.GraphicsContext.CommandList.BeginProfile(Color.Red, "LightProbes");

                    int lightProbeIndex = 0;
                    foreach (var entity in context.SceneSystem.SceneInstance)
                    {
                        var lightProbe = entity.Get<LightProbeComponent>();
                        if (lightProbe == null)
                            continue;

                        var lightProbePosition = lightProbe.Entity.Transform.WorldMatrix.TranslationVector;
                        context.GraphicsContext.ResourceGroupAllocator.Reset(context.GraphicsContext.CommandList);

                        context.GraphicsContext.CommandList.BeginProfile(Color.Red, $"LightProbes {lightProbeIndex}");
                        lightProbeIndex++;

                        cubemapRenderer.Draw(lightProbePosition, cubeTexture);

                        context.GraphicsContext.CommandList.BeginProfile(Color.Red, "Prefilter SphericalHarmonics");

                        // Compute SH coefficients
                        lambertFiltering.Draw(cubemapRenderer.DrawContext);

                        var coefficients = lambertFiltering.PrefilteredLambertianSH.Coefficients;
                        var lightProbeCoefficients = new FastList<Color3>();
                        for (int i = 0; i < coefficients.Length; i++)
                        {
                            lightProbeCoefficients.Add(coefficients[i] * SphericalHarmonics.BaseCoefficients[i]);
                        }

                        lightProbesCoefficients.Add(lightProbe, lightProbeCoefficients);

                        context.GraphicsContext.CommandList.EndProfile(); // Prefilter SphericalHarmonics

                        context.GraphicsContext.CommandList.EndProfile(); // Face XXX

                        // Debug render
                    }

                    context.GraphicsContext.CommandList.EndProfile(); // LightProbes
                }

                cubeTexture.Dispose();

                return lightProbesCoefficients;
            }
        }

        public static unsafe void UpdateCoefficients(LightProbeRuntimeData runtimeData)
        {
            fixed (Color3* destColors = runtimeData.Coefficients)
            {
                for (var lightProbeIndex = 0; lightProbeIndex < runtimeData.LightProbes.Length; lightProbeIndex++)
                {
                    var lightProbe = runtimeData.LightProbes[lightProbeIndex] as LightProbeComponent;

                    // Copy coefficients
                    if (lightProbe?.Coefficients != null)
                    {
                        var lightProbeCoefStart = lightProbeIndex * LambertHamonicOrder * LambertHamonicOrder;
                        for (var index = 0; index < LambertHamonicOrder * LambertHamonicOrder; index++)
                        {
                            destColors[lightProbeCoefStart + index] = index < lightProbe.Coefficients.Count ? lightProbe.Coefficients[index] : new Color3();
                        }
                    }
                }
            }
        }

        public static unsafe LightProbeRuntimeData GenerateRuntimeData(FastList<LightProbeComponent> lightProbes)
        {
            // TODO: Better check: coplanar, etc... (maybe the check inside BowyerWatsonTetrahedralization might be enough -- tetrahedron won't be in positive order)
            if (lightProbes.Count < 4)
                throw new InvalidOperationException("Can't generate lightprobes if less than 4 of them exists.");

            var lightProbePositions = new FastList<Vector3>();
            var lightProbeCoefficients = new Color3[lightProbes.Count * LambertHamonicOrder * LambertHamonicOrder];
            fixed (Color3* destColors = lightProbeCoefficients)
            {
                for (var lightProbeIndex = 0; lightProbeIndex < lightProbes.Count; lightProbeIndex++)
                {
                    var lightProbe = lightProbes[lightProbeIndex];

                    // Copy light position
                    lightProbePositions.Add(lightProbe.Entity.Transform.WorldMatrix.TranslationVector);

                    // Copy coefficients
                    if (lightProbe.Coefficients != null)
                    {
                        var lightProbeCoefStart = lightProbeIndex * LambertHamonicOrder * LambertHamonicOrder;
                        for (var index = 0; index < LambertHamonicOrder * LambertHamonicOrder; index++)
                        {
                            destColors[lightProbeCoefStart + index] = index < lightProbe.Coefficients.Count ? lightProbe.Coefficients[index] : new Color3();
                        }
                    }
                }
            }

            // Generate light probe structure
            var tetra = new BowyerWatsonTetrahedralization();
            var tetraResult = tetra.Compute(lightProbePositions);

            var matrices = new Vector4[tetraResult.Tetrahedra.Count * 3];
            var probeIndices = new Int4[tetraResult.Tetrahedra.Count];

            // Prepare data for GPU: matrices and indices
            for (int i = 0; i < tetraResult.Tetrahedra.Count; ++i)
            {
                var tetrahedron = tetraResult.Tetrahedra[i];
                var tetrahedronMatrix = Matrix.Identity;

                // Compute the tetrahedron matrix
                // https://en.wikipedia.org/wiki/Barycentric_coordinate_system#Barycentric_coordinates_on_tetrahedra
                var vertex3 = tetraResult.Vertices[tetrahedron.Vertices[3]];
                *((Vector3*)&tetrahedronMatrix.M11) = tetraResult.Vertices[tetrahedron.Vertices[0]] - vertex3;
                *((Vector3*)&tetrahedronMatrix.M12) = tetraResult.Vertices[tetrahedron.Vertices[1]] - vertex3;
                *((Vector3*)&tetrahedronMatrix.M13) = tetraResult.Vertices[tetrahedron.Vertices[2]] - vertex3;
                tetrahedronMatrix.Invert(); // TODO: Optimize 3x3 invert

                tetrahedronMatrix.Transpose();

                // Store position of last vertex in last row
                tetrahedronMatrix.M41 = vertex3.X;
                tetrahedronMatrix.M42 = vertex3.Y;
                tetrahedronMatrix.M43 = vertex3.Z;

                matrices[i * 3 + 0] = tetrahedronMatrix.Column1;
                matrices[i * 3 + 1] = tetrahedronMatrix.Column2;
                matrices[i * 3 + 2] = tetrahedronMatrix.Column3;

                probeIndices[i] = *(Int4*)tetrahedron.Vertices;
            }

            var lightProbesCopy = new object[lightProbes.Count];
            for (int i = 0; i < lightProbes.Count; ++i)
                lightProbesCopy[i] = lightProbes[i];

            var result = new LightProbeRuntimeData
            {
                LightProbes = lightProbesCopy,
                Vertices = tetraResult.Vertices,
                UserVertexCount = tetraResult.UserVertexCount,
                Tetrahedra = tetraResult.Tetrahedra,
                Faces = tetraResult.Faces,

                Coefficients = lightProbeCoefficients,
                Matrices = matrices,
                LightProbeIndices = probeIndices,
            };

            return result;
        }
    }
}
