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
            InternalShape = new BulletSharp.HeightfieldShape(heightStickWidth, heightStickLength, dynamicFieldData.Pointer, heightScale, minHeight, maxHeight, 1, (int)BulletPhyScalarType.PhyShort, flipQuadEdges)
            {
                LocalScaling = cachedScaling,
            };
            ShortArray = dynamicFieldData;
        }

        public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, UnmanagedArray<byte> dynamicFieldData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
        {
            cachedScaling = Vector3.One;
            InternalShape = new BulletSharp.HeightfieldShape(heightStickWidth, heightStickLength, dynamicFieldData.Pointer, heightScale, minHeight, maxHeight, 1, (int)BulletPhyScalarType.PhyUchar, flipQuadEdges)
            {
                LocalScaling = cachedScaling,
            };
            ByteArray = dynamicFieldData;
        }

        public HeightfieldColliderShape(int heightStickWidth, int heightStickLength, UnmanagedArray<float> dynamicFieldData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
        {
            cachedScaling = Vector3.One;
            InternalShape = new BulletSharp.HeightfieldShape(heightStickWidth, heightStickLength, dynamicFieldData.Pointer, heightScale, minHeight, maxHeight, 1, (int)BulletPhyScalarType.PhyFloat, flipQuadEdges)
            {
                LocalScaling = cachedScaling,
            };
            FloatArray = dynamicFieldData;
        }

        public bool UseDiamondSubdivision
        {
            set { ((BulletSharp.HeightfieldShape)InternalShape).SetUseDiamondSubdivision(value); }
        }

        public bool UseZigzagSubdivision
        {
            set { ((BulletSharp.HeightfieldShape)InternalShape).SetUseZigzagSubdivision(value); }
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
            PhyFloat,
            PhyDouble,
            PhyInteger,
            PhyShort,
            PhyFixedpoint88,
            PhyUchar,
        }
    }
}
