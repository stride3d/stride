// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<HeightfieldColliderShapeDesc>))]
    [DataContract("HeightfieldColliderShapeDesc")]
    [Display(300, "Heightfield")]
    public class HeightfieldColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <summary>
        /// The source to initialize the height stick array.
        /// </summary>
        [DataMember(10)]
        [NotNull]
        [Display("Source", Expand = ExpandRule.Always)]
        public IHeightStickArraySource HeightStickArraySource { get; set; } = new HeightStickArraySourceFromHeightmap();

        [DataMember(70)]
        public bool FlipQuadEdges = false;

        /// <summary>
        /// Add a value to local offset in order to center specific height.
        /// </summary>
        /// <remarks>
        /// NOTE: The center height is the middle point of the range, if this is disabled with asymmetrical range.
        /// </remarks>
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

            var sourceMatch = other.HeightStickArraySource?.Match(HeightStickArraySource) ?? HeightStickArraySource == null;

            var centeringMatch = other.Centering.Match(Centering);

            return sourceMatch &&
                centeringMatch &&
                other.FlipQuadEdges == FlipQuadEdges;
        }

        /// <summary>
        /// Get the offset required in order to center specific height.
        /// </summary>
        /// <param name="heightRange">The range of the height.</param>
        /// <param name="centerHeight">The height to be centered.</param>
        /// <returns>The value in y axis required in order to center specific height.</returns>
        public static float GetCenteringOffset(Vector2 heightRange, float centerHeight)
        {
            return (heightRange.X + heightRange.Y) * 0.5f - centerHeight;
        }

        private static UnmanagedArray<T> CreateHeights<T>(IHeightStickArraySource heightStickArraySource) where T : struct
        {
            if (!heightStickArraySource?.IsValid() ?? false)
            {
                return null;
            }

            var arrayLength = heightStickArraySource.HeightStickSize.X * heightStickArraySource.HeightStickSize.Y;

            var unmanagedArray = new UnmanagedArray<T>(arrayLength);

            heightStickArraySource.CopyTo(unmanagedArray, 0);

            return unmanagedArray;
        }

        /// <summary>
        /// Get the centering offset that will be added to the local offset of the collider shape.
        /// </summary>
        /// <returns>The value that will be added to the local offset of the collider shape in order to center specific height.</returns>
        public float GetCenteringOffset()
        {
            if (HeightStickArraySource == null) throw new InvalidOperationException($"{ nameof(HeightStickArraySource) } is a null.");

            return Centering.Enabled ?
                GetCenteringOffset(HeightStickArraySource.HeightRange, Centering.CenterHeight) :
                0f;
        }

        public ColliderShape CreateShape()
        {
            object unmanagedArray;

            switch (HeightStickArraySource.HeightType)
            {
                case HeightfieldTypes.Float:
                {
                    unmanagedArray = CreateHeights<float>(HeightStickArraySource);
                    break;
                }
                case HeightfieldTypes.Short:
                {
                    unmanagedArray = CreateHeights<short>(HeightStickArraySource);
                    break;
                }
                case HeightfieldTypes.Byte:
                {
                    unmanagedArray = CreateHeights<byte>(HeightStickArraySource);
                    break;
                }

                default:
                    return null;
            }

            if (unmanagedArray == null) return null;

            var shape = new HeightfieldColliderShape
                        (
                            HeightStickArraySource.HeightStickSize.X,
                            HeightStickArraySource.HeightStickSize.Y,
                            HeightStickArraySource.HeightType,
                            unmanagedArray,
                            HeightStickArraySource.HeightScale,
                            HeightStickArraySource.HeightRange.X,
                            HeightStickArraySource.HeightRange.Y,
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
