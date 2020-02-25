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
        public HeightfieldCenteringParameters Centering { get; set; } = new HeightfieldCenteringParameters
        {
            Enabled = true,
            CenterHeight = 0,
        };

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

            var initialHeightsMatch = other.InitialHeights?.Match(InitialHeights) ?? InitialHeights == null;

            var centeringMatch = other.Centering.Match(Centering);

            return initialHeightsMatch &&
                centeringMatch &&
                other.FlipQuadEdges == FlipQuadEdges;
        }

        public static bool IsValidHeightStickSize(Int2 size)
        {
            return size.X >= HeightfieldColliderShape.MinimumHeightStickWidth && size.Y >= HeightfieldColliderShape.MinimumHeightStickLength;
        }

        public static float GetCenteringOffset(Vector2 heightRange, float centerHeight)
        {
            return (heightRange.X + heightRange.Y) * 0.5f - centerHeight;
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

        public float GetCenteringOffset()
        {
            if (InitialHeights == null) throw new InvalidOperationException($"{ nameof(InitialHeights) } is a null.");

            return Centering.Enabled ?
                GetCenteringOffset(InitialHeights.HeightRange, Centering.CenterHeight) :
                0f;
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
                            LocalOffset = LocalOffset + new Vector3(0, GetCenteringOffset(), 0),
                            LocalRotation = LocalRotation,
                        };

            return shape;
        }
    }
}
