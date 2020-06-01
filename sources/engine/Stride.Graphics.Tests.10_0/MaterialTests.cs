// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// Test <see cref="Material"/>.
    /// </summary>
    public class MaterialTests : GameTestBase
    {
        private string testName;
        private Func<MaterialTests, Material> createMaterial;

        public MaterialTests() : this(null)
        {
        }

        private MaterialTests(Func<MaterialTests, Material> createMaterial)
        {
            this.createMaterial = createMaterial;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_10_0 };
        }

        protected override void PrepareContext()
        {
            base.PrepareContext();

            // Override initial scene
            SceneSystem.InitialSceneUrl = "MaterialTests/MaterialScene";
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Load default graphics compositor
            SceneSystem.GraphicsCompositor = Content.Load<GraphicsCompositor>("GraphicsCompositor");

            var cube = SceneSystem.SceneInstance.First(x => x.Name == "Cube");
            var sphere = SceneSystem.SceneInstance.First(x => x.Name == "Sphere");

            var camera = SceneSystem.SceneInstance.First(x => x.Name == "Camera");
            if (camera != null)
            {
                var cameraScript = new FpsTestCamera();
                camera.Add(cameraScript);
            }

            var material = createMaterial(this);

            // Apply it on both cube and sphere
            cube.Get<ModelComponent>().Model.Materials[0] = material;
            sphere.Get<ModelComponent>().Model.Materials[0] = material;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            // Take screenshot first frame
            FrameGameSystem.TakeScreenshot(null, testName);
        }

        #region Basic tests (diffuse color/float4)
        [Fact]
        public void MaterialDiffuseColor()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Base/MaterialDiffuseColor")) { TestName = nameof(MaterialDiffuseColor) });
        }

        [Fact]
        public void MaterialDiffuseFloat4()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Base/MaterialDiffuseFloat4")) { TestName = nameof(MaterialDiffuseFloat4) });
        }
        #endregion

        #region Test Diffuse ComputeTextureColor with various parameters
        [Fact]
        public void MaterialDiffuseTexture()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Base/MaterialDiffuseTexture")) { TestName = nameof(MaterialDiffuseTexture) });
        }

        // Test ComputeTextureColor.Fallback
        [Fact]
        public void MaterialDiffuseTextureFallback()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Base/MaterialDiffuseTextureFallback")) { TestName = nameof(MaterialDiffuseTextureFallback) });
        }

        // Test texcoord offsets
        [Fact]
        public void MaterialDiffuseTextureOffset()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Base/MaterialDiffuseTextureOffset")) { TestName = nameof(MaterialDiffuseTextureOffset) });
        }

        // Test texcoord scaling
        [Fact]
        public void MaterialDiffuseTextureScaled()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Base/MaterialDiffuseTextureScaled")) { TestName = nameof(MaterialDiffuseTextureScaled) });
        }

        // Test texcoord1
        [Fact]
        public void MaterialDiffuseTextureCoord1()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Base/MaterialDiffuseTextureCoord1")) { TestName = nameof(MaterialDiffuseTextureCoord1) });
        }

        // Test uv address modes
        [Fact]
        public void MaterialDiffuseTextureClampMirror()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Base/MaterialDiffuseTextureClampMirror")) { TestName = nameof(MaterialDiffuseTextureClampMirror) });
        }
        #endregion

        #region Test diffuse binary operators
        [Fact]
        public void MaterialBinaryOperatorMultiply()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/BinaryOperators/MaterialBinaryOperatorMultiply")) { TestName = nameof(MaterialBinaryOperatorMultiply) });
        }

        [Fact]
        public void MaterialBinaryOperatorAdd()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/BinaryOperators/MaterialBinaryOperatorAdd")) { TestName = nameof(MaterialBinaryOperatorAdd) });
        }
        #endregion

        #region Test diffuse compute color
        [Fact]
        public void MaterialDiffuseComputeColorFixed()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/ComputeColors/MaterialDiffuseComputeColorFixed")) { TestName = nameof(MaterialDiffuseComputeColorFixed) });
        }
        #endregion

        #region Test material features (specular, metalness, cavity, normal map, emissive)
        [Fact]
        public void MaterialMetalness()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Features/MaterialMetalness")) { TestName = nameof(MaterialMetalness) });
        }

        [Fact]
        public void MaterialSpecular()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Features/MaterialSpecular")) { TestName = nameof(MaterialSpecular) });
        }

        [Fact]
        public void MaterialNormalMap()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Features/MaterialNormalMap")) { TestName = nameof(MaterialNormalMap) });
        }

        [Fact]
        public void MaterialNormalMapCompressed()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Features/MaterialNormalMapCompressed")) { TestName = nameof(MaterialNormalMapCompressed) });
        }

        [Fact]
        public void MaterialEmissive()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Features/MaterialEmissive")) { TestName = nameof(MaterialEmissive) });
        }

        [Fact]
        public void MaterialCavity()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Features/MaterialCavity")) { TestName = nameof(MaterialCavity) });
        }
        #endregion

        #region Test layers with different shading models
        // Layers (A, B and C are shading models; first character is root parent, and next characters are its child)
        [Fact]
        public void MaterialLayerAAA()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Layers/MaterialLayerAAA")) { TestName = nameof(MaterialLayerAAA) });
        }

        [Fact(Skip = "Disabled until XK-3123 is fixed (material blending SM flush results in layer masks applied improperly)")]
        public void MaterialLayerABB()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Layers/MaterialLayerABB")) { TestName = nameof(MaterialLayerABB) });
        }

        [Fact]
        public void MaterialLayerABA()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Layers/MaterialLayerABA")) { TestName = nameof(MaterialLayerABA) });
        }

        [Fact]
        public void MaterialLayerABC()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Layers/MaterialLayerABC")) { TestName = nameof(MaterialLayerABC) });
        }

        [Fact(Skip = "Disabled until XK-3123 is fixed (material blending SM flush results in layer masks applied improperly)")]
        public void MaterialLayerBAA()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Layers/MaterialLayerBAA")) { TestName = nameof(MaterialLayerBAA) });
        }

        [Fact]
        public void MaterialLayerBBB()
        {
            RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Layers/MaterialLayerBBB")) { TestName = nameof(MaterialLayerBBB) });
        }

        [Fact(Skip = "Similar to MaterialLayerABB but using API for easier debugging")]
        public void MaterialLayerABBWithAPI()
        {
            //RunGameTest(new MaterialTests(game => game.Content.Load<Material>("MaterialTests/Layers/MaterialLayerABB")) { TestName = nameof(MaterialLayerABBWithAPI) });
            RunGameTest(new MaterialTests(game =>
            {
                // Use same gold as MaterialLayerABB
                game.testName = typeof(MaterialTests).FullName + "." + nameof(MaterialLayerABB);

                var layerMask = game.Content.Load<Texture>("MaterialTests/Layers/LayerMask");
                var layerMask2 = game.Content.Load<Texture>("MaterialTests/Layers/LayerMask2");

                var diffuse = game.Content.Load<Texture>("MaterialTests/stone4_dif");

                var context = new MaterialGeneratorContextExtended();

                // Load material
                var materialDesc = new MaterialDescriptor
                {
                    Attributes =
                        {
                            Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor { Texture = diffuse }),
                            DiffuseModel = new MaterialDiffuseLambertModelFeature()
                        },
                    Layers =
                        {
                            new MaterialBlendLayer()
                            {
                                BlendMap = new ComputeTextureScalar { Texture = layerMask, Filtering = TextureFilter.Point },
                                Material = context.MapTo(new Material(), new MaterialDescriptor() // MaterialB1
                                {
                                    Attributes =
                                    {
                                        Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.Blue)),
                                        DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                                        Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0.2f)),
                                        SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                                        MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.4f)),
                                    },
                                }),
                            },
                            new MaterialBlendLayer()
                            {
                                BlendMap = new ComputeTextureScalar { Texture = layerMask2, Filtering = TextureFilter.Point },
                                Material = context.MapTo(new Material(), new MaterialDescriptor() // MaterialB2
                                {
                                    Attributes =
                                    {
                                        Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.Red)),
                                        DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                                        Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0.8f)),
                                        SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                                        MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.9f)),
                                    },
                                }),
                            },
                        },
                };

                return CreateMaterial(materialDesc, context);
            })
            {
                TestName = nameof(MaterialLayerABBWithAPI)
            });
        }

        #endregion

        private static Material CreateMaterial(MaterialDescriptor materialDesc, MaterialGeneratorContextExtended context)
        {
            var result = MaterialGenerator.Generate(materialDesc, context, "test_material");

            if (result.HasErrors)
                throw new InvalidOperationException($"Error compiling material:\n{result.ToText()}");

            return result.Material;
        }

        private class MaterialGeneratorContextExtended : MaterialGeneratorContext
        {
            private readonly Dictionary<object, object> assetMap = new Dictionary<object, object>();

            public MaterialGeneratorContextExtended() : base(null)
            {
                FindAsset = asset =>
                {
                    object value;
                    Assert.True(assetMap.TryGetValue(asset, out value), "A material instance has not been associated to a MaterialDescriptor");
                    return value;
                };
            }

            public T MapTo<T>(T runtime, object asset)
            {
                assetMap[runtime] = asset;
                return runtime;
            }
        }
    }
}
