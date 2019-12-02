// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
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
        public Heightmap InitialHeights { get; set; }

        [DataMember(30)]
        public HeightfieldTypes HeightfieldType;

        [DataMember(40)]
        public Int2 HeightStickSize;

        [DataMember(50)]
        public Vector2 HeightRange;

        [DataMember(60)]
        [NotNull]
        public CustomHeightScale HeightScale { get; set; }

        [DataMember(70)]
        public bool FlipQuadEdges;

        [DataMember(100)]
        public Vector3 LocalOffset;

        [DataMember(110)]
        public Quaternion LocalRotation;

        public HeightfieldColliderShapeDesc()
        {
            InitialHeights = null;
            HeightfieldType = HeightfieldTypes.Float;
            HeightStickSize = new Int2(65, 65);
            HeightRange = new Vector2(-10, 10);
            HeightScale = new CustomHeightScale();
            FlipQuadEdges = false;
            LocalOffset = new Vector3(0, 0, 0);
            LocalRotation = Quaternion.Identity;
        }

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

            var heightScaleComparison = (other.HeightScale.Enabled && HeightScale.Enabled) ?
                Math.Abs(other.HeightScale.Scale - HeightScale.Scale) < float.Epsilon :
                other.HeightScale.Enabled == HeightScale.Enabled;

            var initialHeightsComparison = (other.InitialHeights == InitialHeights);

            return initialHeightsComparison &&
                other.HeightfieldType == HeightfieldType &&
                other.HeightStickSize == HeightStickSize &&
                other.HeightRange == HeightRange &&
                heightScaleComparison &&
                other.FlipQuadEdges == FlipQuadEdges;
        }

        private static void FillHeights<T>(UnmanagedArray<T> unmanagedArray, T value) where T : struct
        {
            if (unmanagedArray == null) throw new ArgumentNullException(nameof(unmanagedArray));

            for (int i = 0; i < unmanagedArray.Length; ++i)
            {
                unmanagedArray[i] = value;
            }
        }

        private static UnmanagedArray<T> CreateHeights<T>(int length, T[] initialHeights) where T : struct
        {
            var unmanagedArray = new UnmanagedArray<T>(length);

            if (initialHeights != null)
            {
                unmanagedArray.Write(initialHeights, 0, 0, Math.Min(unmanagedArray.Length, initialHeights.Length));
            }
            else
            {
                FillHeights(unmanagedArray, default);
            }

            return unmanagedArray;
        }

        public ColliderShape CreateShape()
        {
                if (HeightStickSize.X <= 1 ||
                    HeightStickSize.Y <= 1 ||
                    HeightRange.Y < HeightRange.X ||
                    Math.Abs(HeightRange.Y - HeightRange.X) < float.Epsilon)
                {
                    return null;
                }

                float heightScale = (HeightfieldType != HeightfieldTypes.Float) && HeightScale.Enabled ? HeightScale.Scale : CalculateHeightScale();

                if (Math.Abs(heightScale) < float.Epsilon)
                {
                    return null;
                }

                var arrayLength = HeightStickSize.X * HeightStickSize.Y;

                object unmanagedArray;

                switch (HeightfieldType)
                {
                    case HeightfieldTypes.Float:
                    {
                        unmanagedArray = CreateHeights(arrayLength, InitialHeights?.Floats);
                        break;
                    }
                    case HeightfieldTypes.Short:
                    {
                        unmanagedArray = CreateHeights(arrayLength, InitialHeights?.Shorts);
                        break;
                    }
                    case HeightfieldTypes.Byte:
                    {
                        unmanagedArray = CreateHeights(arrayLength, InitialHeights?.Bytes);
                        break;
                    }

                    default:
                        return null;
                }

                var shape = new HeightfieldColliderShape
                            (
                                HeightStickSize.X,
                                HeightStickSize.Y,
                                HeightfieldType,
                                unmanagedArray,
                                heightScale,
                                HeightRange.X,
                                HeightRange.Y,
                                FlipQuadEdges
                            )
                            {
                                LocalOffset = LocalOffset,
                                LocalRotation = LocalRotation,
                            };

                return shape;
        }

        public float CalculateHeightScale()
        {
            switch (HeightfieldType)
            {
                case HeightfieldTypes.Float:
                    return 1f;

                case HeightfieldTypes.Short:
                    return Math.Max(Math.Abs(HeightRange.X), Math.Abs(HeightRange.Y)) / short.MaxValue;

                case HeightfieldTypes.Byte:
                    return HeightRange.Y / byte.MaxValue;

                default:
                    return 0f;
            }
        }

        [DataContract]
        public class CustomHeightScale
        {
            [DataMember(0)]
            [DefaultValue(false)]
            public bool Enabled { get; set; }

            [DataMember(10)]
            [InlineProperty]
            public float Scale { get; set; }

            public CustomHeightScale()
                : this(1f)
            {
            }

            public CustomHeightScale(float value)
            {
                Scale = value;
            }
        }
    }
}
