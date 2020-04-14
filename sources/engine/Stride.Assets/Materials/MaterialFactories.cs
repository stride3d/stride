// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Assets.Materials
{
    public class DiffuseMaterialFactory : AssetFactory<MaterialAsset>
    {
        public static MaterialAsset Create()
        {
            var material = new MaterialAsset
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature
                    {
                        DiffuseMap = new ComputeTextureColor
                        {
                            FallbackValue = new ComputeColor(new Color(255, 214, 111))
                        }
                    },
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                }
            };
            return material;
        }

        public override MaterialAsset New()
        {
            return Create();
        }
    }

    public class SpecularMaterialFactory : AssetFactory<MaterialAsset>
    {
        public static MaterialAsset Create()
        {
            var material = new MaterialAsset
            {
                Attributes =
                {
                    MicroSurface = new MaterialGlossinessMapFeature
                    {
                        GlossinessMap = new ComputeFloat(0.6f)
                    },
                    Diffuse = new MaterialDiffuseMapFeature
                    {
                        DiffuseMap = new ComputeTextureColor
                        {
                            FallbackValue = new ComputeColor(new Color(255, 214, 111))
                        }
                    },
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular = new MaterialSpecularMapFeature
                    {
                        SpecularMap = new ComputeColor(Color4.White)
                    },
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature()
                }
            };

            return material;
        }

        public override MaterialAsset New()
        {
            return Create();
        }
    }

    public class MetalnessMaterialFactory : AssetFactory<MaterialAsset>
    {
        public static MaterialAsset Create()
        {
            var material = new MaterialAsset
            {
                Attributes =
                {
                    MicroSurface = new MaterialGlossinessMapFeature
                    {
                        GlossinessMap = new ComputeFloat(0.6f)
                    },
                    Diffuse = new MaterialDiffuseMapFeature
                    {
                        DiffuseMap = new ComputeTextureColor
                        {
                            // This is gold
                            FallbackValue = new ComputeColor(new Color4(1.0f, 0.88565079f, 0.609162496f, 1.0f))
                        }
                    },
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular = new MaterialMetalnessMapFeature
                    {
                        MetalnessMap = new ComputeFloat(1.0f)
                    },
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature()
                }
            };

            return material;
        }

        public override MaterialAsset New()
        {
            return Create();
        }
    }

    public class GlassMaterialFactory : AssetFactory<MaterialAsset>
    {
        public static MaterialAsset Create()
        {
            var material = new MaterialAsset
            {
                Attributes =
                {
                    MicroSurface = new MaterialGlossinessMapFeature
                    {
                        GlossinessMap = new ComputeFloat(0.95f)
                    },
                    Diffuse = new MaterialDiffuseMapFeature
                    {
                        DiffuseMap = new ComputeColor(new Color4(0.8f, 0.8f, 0.8f, 1.0f))
                    },
                    DiffuseModel = null,
                    Specular = new MaterialMetalnessMapFeature
                    {
                        MetalnessMap = new ComputeFloat(0.0f)
                    },
                    SpecularModel = new MaterialSpecularThinGlassModelFeature()
                }
            };

            return material;
        }

        public override MaterialAsset New()
        {
            return Create();
        }
    }

    public class ClearCoatMaterialFactory : AssetFactory<MaterialAsset>
    {
        public static MaterialAsset Create()
        {
            // Load default texture assets
            var clearCoatLayerNormalMap = new ComputeTextureColor
            {
                Texture = AttachedReferenceManager.CreateProxyObject<Texture>(new AssetId("2f76bcba-ae9f-4954-b98d-f94c2102ff86"), "StrideCarPaintOrangePeelNM"),
                Scale = new Vector2(8, 8)
            };
            
            var metalFlakesNormalMap = new ComputeTextureColor
            {
                Texture = AttachedReferenceManager.CreateProxyObject<Texture>(new AssetId("7e2761d1-ef86-420a-b7a7-a0ed1c16f9bb"), "StrideCarPaintMetalFlakesNM"),
                Scale = new Vector2(128, 128),
                UseRandomTextureCoordinates = true
            };

            var metalFlakesMask = new ComputeTextureScalar
            {
                Texture = AttachedReferenceManager.CreateProxyObject<Texture>(new AssetId("7e2761d1-ef86-420a-b7a7-a0ed1c16f9bb"), "StrideCarPaintMetalFlakesNM"),
                Scale = new Vector2(128, 128),
                UseRandomTextureCoordinates = true
            };

            // Red Paint
            // Color4 defaultCarPaintColor = new Color4(0.274509817f, 0.003921569f, 0.0470588244f, 1.0f);
            // Color4 defaultMetalFlakesColor = new Color4(defaultCarPaintColor.R * 2.0f, defaultCarPaintColor.G * 2.0f, defaultCarPaintColor.B * 2.0f, 1.0f);

            // Blue Paint
            Color4 defaultPaintColor = new Color4(0, 0.09411765f, 0.329411775f, 1.0f);
            Color4 defaultMetalFlakesColor = new Color4(0, 0.180392161f, 0.6313726f, 1.0f);

            var material = new MaterialAsset
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color4.Black)),
                    Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0.0f)),

                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature(),

                    ClearCoat = new MaterialClearCoatFeature
                    {
                        // Base Layer
                        BasePaintDiffuseMap = new ComputeColor(defaultPaintColor),
                        BasePaintGlossinessMap = new ComputeBinaryScalar(new ComputeFloat(0.50f), metalFlakesMask, BinaryOperator.Multiply),

                        // Metal Flakes Layer
                        MetalFlakesDiffuseMap = new ComputeColor(defaultMetalFlakesColor),
                        MetalFlakesGlossinessMap = new ComputeBinaryScalar(new ComputeFloat(1.00f), metalFlakesMask, BinaryOperator.Multiply),
                        MetalFlakesMetalnessMap = new ComputeFloat(1.00f),

                        MetalFlakesNormalMap = metalFlakesNormalMap,
                        MetalFlakesScaleAndBias = true,

                        // Clear coat layer
                        ClearCoatGlossinessMap = new ComputeFloat(1.00f),
                        ClearCoatMetalnessMap = new ComputeFloat(0.50f),

                        OrangePeelNormalMap = clearCoatLayerNormalMap,
                        OrangePeelScaleAndBias = true,
                    }
                }
            };

            return material;
        }

        public override MaterialAsset New()
        {
            return Create();
        }
    }
}
