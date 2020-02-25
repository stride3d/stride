// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<HeightfieldColliderShapeDesc>))]
    [DataContract("HeightfieldColliderShapeDesc")]
    [Display(300, "Heightfield")]
    public class HeightfieldColliderShapeDesc : IInlineColliderShapeDesc
    {
        [DataMember(10)]
        [NotNull]
        [Display(Expand = ExpandRule.Always)]
        public IHeightStickArraySource InitialHeights { get; set; } = new HeightStickArraySourceFromHeightmap();

        [DataMember(70)]
        public bool FlipQuadEdges = false;

        [DataMember(80)]
        [Display("Center the height 0")]
        public bool ShouldCenterHeightZero = true;

        [DataMember(100)]
        public Vector3 LocalOffset;

        [DataMember(110)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public bool Match(object obj)
        {
            var other = obj as HeightfieldColliderShapeDesc;

            if (other == null)
            {
                return false;
            }

            if (LocalOffset != other.LocalOffset || LocalRotation != other.LocalRotation)
            {
                return false;
            }

            var initialHeightsComparison = other.InitialHeights?.Match(InitialHeights) ?? InitialHeights == null;

            return initialHeightsComparison &&
                other.FlipQuadEdges == FlipQuadEdges &&
                other.ShouldCenterHeightZero == ShouldCenterHeightZero;
        }

        public static bool IsValidHeightStickSize(Int2 size)
        {
            return size.X >= HeightfieldColliderShape.MinimumHeightStickWidth && size.Y >= HeightfieldColliderShape.MinimumHeightStickLength;
        }

        private static void FillHeights<T>(UnmanagedArray<T> unmanagedArray, T value) where T : struct
        {
            if (unmanagedArray == null) throw new ArgumentNullException(nameof(unmanagedArray));

            for (int i = 0; i < unmanagedArray.Length; ++i)
            {
                unmanagedArray[i] = value;
            }
        }

        private static UnmanagedArray<T> CreateHeights<T>(IHeightStickArraySource heightStickArraySource) where T : struct
        {
            if (heightStickArraySource == null) throw new ArgumentNullException(nameof(heightStickArraySource));

            var arrayLength = heightStickArraySource.HeightStickSize.X * heightStickArraySource.HeightStickSize.Y;

            var unmanagedArray = new UnmanagedArray<T>(arrayLength);

            heightStickArraySource.CopyTo(unmanagedArray, 0);

            return unmanagedArray;
        }

        public ColliderShape CreateShape()
        {
            if (InitialHeights == null ||
                !IsValidHeightStickSize(InitialHeights.HeightStickSize) ||
                InitialHeights.HeightRange.Y < InitialHeights.HeightRange.X ||
                Math.Abs(InitialHeights.HeightRange.Y - InitialHeights.HeightRange.X) < float.Epsilon)
            {
                return null;
            }

            object unmanagedArray;

            switch (InitialHeights.HeightType)
            {
                case HeightfieldTypes.Float:
                {
                    unmanagedArray = CreateHeights<float>(InitialHeights);
                    break;
                }
                case HeightfieldTypes.Short:
                {
                    unmanagedArray = CreateHeights<short>(InitialHeights);
                    break;
                }
                case HeightfieldTypes.Byte:
                {
                    unmanagedArray = CreateHeights<byte>(InitialHeights);
                    break;
                }

                default:
                    return null;
            }

            var offsetToCenterHeightZero = ShouldCenterHeightZero ? InitialHeights.HeightRange.X + ((InitialHeights.HeightRange.Y - InitialHeights.HeightRange.X) * 0.5f) : 0f;

            var shape = new HeightfieldColliderShape
                        (
                            InitialHeights.HeightStickSize.X,
                            InitialHeights.HeightStickSize.Y,
                            InitialHeights.HeightType,
                            unmanagedArray,
                            InitialHeights.HeightScale,
                            InitialHeights.HeightRange.X,
                            InitialHeights.HeightRange.Y,
                            FlipQuadEdges
                        )
                        {
                            LocalOffset = LocalOffset + new Vector3(0, offsetToCenterHeightZero, 0),
                            LocalRotation = LocalRotation,
                        };

            return shape;
        }
    }
}
