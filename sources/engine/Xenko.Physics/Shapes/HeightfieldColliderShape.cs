// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Physics.Shapes
{
    public class HeightfieldColliderShape : ColliderShape
    {
        public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, UnmanagedArray<short> dynamicFieldData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
        {
            cachedScaling = Vector3.One;
            InternalShape = new BulletSharp.HeightfieldTerrainShape(heightStickWidth, heightStickLength, dynamicFieldData.Pointer, heightScale, minHeight, maxHeight, 1, BulletSharp.PhyScalarType.Int16, flipQuadEdges)
            {
                LocalScaling = cachedScaling,
            };
            ShortArray = dynamicFieldData;
        }

        public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, UnmanagedArray<byte> dynamicFieldData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
        {
            cachedScaling = Vector3.One;
            InternalShape = new BulletSharp.HeightfieldTerrainShape(heightStickWidth, heightStickLength, dynamicFieldData.Pointer, heightScale, minHeight, maxHeight, 1, BulletSharp.PhyScalarType.Byte, flipQuadEdges)
            {
                LocalScaling = cachedScaling,
            };
            ByteArray = dynamicFieldData;
        }

        public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, UnmanagedArray<float> dynamicFieldData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
        {
            cachedScaling = Vector3.One;
            InternalShape = new BulletSharp.HeightfieldTerrainShape(heightStickWidth, heightStickLength, dynamicFieldData.Pointer, heightScale, minHeight, maxHeight, 1, BulletSharp.PhyScalarType.Single, flipQuadEdges)
            {
                LocalScaling = cachedScaling,
            };
            FloatArray = dynamicFieldData;
        }

        public bool UseDiamondSubdivision
        {
            set { ((BulletSharp.HeightfieldTerrainShape)InternalShape).SetUseDiamondSubdivision(value); }
        }

        public bool UseZigzagSubdivision
        {
            set { ((BulletSharp.HeightfieldTerrainShape)InternalShape).SetUseZigzagSubdivision(value); }
        }

        public UnmanagedArray<short> ShortArray { get; private set; }

        public UnmanagedArray<byte> ByteArray { get; private set; }

        public UnmanagedArray<float> FloatArray { get; private set; }

        public override void Dispose()
        {
            base.Dispose();

            ShortArray?.Dispose();
            ShortArray = null;
            ByteArray?.Dispose();
            ByteArray = null;
            FloatArray?.Dispose();
            FloatArray = null;
        }

        private enum BulletPhyScalarType
        {
            PhyFloat = BulletSharp.PhyScalarType.Single,
            PhyDouble = BulletSharp.PhyScalarType.Double,
            PhyInteger = BulletSharp.PhyScalarType.Int32,
            PhyShort = BulletSharp.PhyScalarType.Int16,
            PhyFixedpoint88 = BulletSharp.PhyScalarType.FixedPoint88,
            PhyUchar = BulletSharp.PhyScalarType.Byte,
        }
    }
}
