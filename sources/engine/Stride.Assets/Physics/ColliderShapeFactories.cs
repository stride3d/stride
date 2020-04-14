// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Physics;

namespace Stride.Assets.Physics
{
    public class ColliderShapeBoxFactory : AssetFactory<ColliderShapeAsset>
    {
        public static ColliderShapeAsset Create()
        {
            return new ColliderShapeAsset { ColliderShapes = { new BoxColliderShapeDesc() } };
        }

        public override ColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class ColliderShapeCapsuleFactory : AssetFactory<ColliderShapeAsset>
    {
        public static ColliderShapeAsset Create()
        {
            return new ColliderShapeAsset { ColliderShapes = { new CapsuleColliderShapeDesc() } };
        }

        public override ColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class ColliderShapeConvexHullFactory : AssetFactory<ColliderShapeAsset>
    {
        public static ColliderShapeAsset Create()
        {
            return new ColliderShapeAsset { ColliderShapes = { new ConvexHullColliderShapeDesc() } };
        }

        public override ColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class ColliderShapeCylinderFactory : AssetFactory<ColliderShapeAsset>
    {
        public static ColliderShapeAsset Create()
        {
            return new ColliderShapeAsset { ColliderShapes = { new CylinderColliderShapeDesc() } };
        }

        public override ColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class ColliderShapeConeFactory : AssetFactory<ColliderShapeAsset>
    {
        public static ColliderShapeAsset Create()
        {
            return new ColliderShapeAsset { ColliderShapes = { new ConeColliderShapeDesc() } };
        }

        public override ColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class ColliderShapePlaneFactory : AssetFactory<ColliderShapeAsset>
    {
        public static ColliderShapeAsset Create()
        {
            return new ColliderShapeAsset { ColliderShapes = { new StaticPlaneColliderShapeDesc() } };
        }

        public override ColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class ColliderShapeSphereFactory : AssetFactory<ColliderShapeAsset>
    {
        public static ColliderShapeAsset Create()
        {
            return new ColliderShapeAsset { ColliderShapes = { new SphereColliderShapeDesc() } };
        }

        public override ColliderShapeAsset New()
        {
            return Create();
        }
    }

    public class ColliderShapeHeightfieldFactory : AssetFactory<ColliderShapeAsset>
    {
        public static ColliderShapeAsset Create()
        {
            return new ColliderShapeAsset { ColliderShapes = { new HeightfieldColliderShapeDesc() } };
        }

        public override ColliderShapeAsset New()
        {
            return Create();
        }
    }
}
