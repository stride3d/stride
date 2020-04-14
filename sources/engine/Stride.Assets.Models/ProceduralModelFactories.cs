// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Rendering.ProceduralModels;

namespace Stride.Assets.Models
{
    public class ProceduralModelCapsuleFactory : AssetFactory<ProceduralModelAsset>
    {
        public static ProceduralModelAsset Create()
        {
            return new ProceduralModelAsset { Type = new CapsuleProceduralModel() };
        }

        public override ProceduralModelAsset New()
        {
            return Create();
        }
    }

    public class ProceduralModelConeFactory : AssetFactory<ProceduralModelAsset>
    {
        public static ProceduralModelAsset Create()
        {
            return new ProceduralModelAsset { Type = new ConeProceduralModel() };
        }

        public override ProceduralModelAsset New()
        {
            return Create();
        }
    }

    public class ProceduralModelCubeFactory : AssetFactory<ProceduralModelAsset>
    {
        public static ProceduralModelAsset Create()
        {
            return new ProceduralModelAsset { Type = new CubeProceduralModel() };
        }

        public override ProceduralModelAsset New()
        {
            return Create();
        }
    }

    public class ProceduralModelCylinderFactory : AssetFactory<ProceduralModelAsset>
    {
        public static ProceduralModelAsset Create()
        {
            return new ProceduralModelAsset { Type = new CylinderProceduralModel() };
        }

        public override ProceduralModelAsset New()
        {
            return Create();
        }
    }

    public class ProceduralModelGeoSphereFactory : AssetFactory<ProceduralModelAsset>
    {
        public static ProceduralModelAsset Create()
        {
            return new ProceduralModelAsset { Type = new GeoSphereProceduralModel() };
        }

        public override ProceduralModelAsset New()
        {
            return Create();
        }
    }

    public class ProceduralModelPlaneFactory : AssetFactory<ProceduralModelAsset>
    {
        public static ProceduralModelAsset Create()
        {
            return new ProceduralModelAsset { Type = new PlaneProceduralModel() };
        }

        public override ProceduralModelAsset New()
        {
            return Create();
        }
    }

    public class ProceduralModelSphereFactory : AssetFactory<ProceduralModelAsset>
    {
        public static ProceduralModelAsset Create()
        {
            return new ProceduralModelAsset { Type = new SphereProceduralModel() };
        }

        public override ProceduralModelAsset New()
        {
            return Create();
        }
    }

    public class ProceduralModelTeapotFactory : AssetFactory<ProceduralModelAsset>
    {
        public static ProceduralModelAsset Create()
        {
            return new ProceduralModelAsset { Type = new TeapotProceduralModel() };
        }

        public override ProceduralModelAsset New()
        {
            return Create();
        }
    }

    public class ProceduralModelTorusFactory : AssetFactory<ProceduralModelAsset>
    {
        public static ProceduralModelAsset Create()
        {
            return new ProceduralModelAsset { Type = new TorusProceduralModel() };
        }

        public override ProceduralModelAsset New()
        {
            return Create();
        }
    }
}
