// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Mathematics;

namespace Stride.Physics
{
    public class HeightmapUtils
    {
        public static bool CheckHeightParameters(Int2 size, HeightfieldTypes heightType, Vector2 heightRange, float heightScale, bool throwExceptionWhenInvalid)
        {
            try
            {
                // Size

                if (size.X < HeightfieldColliderShape.MinimumHeightStickWidth || size.Y < HeightfieldColliderShape.MinimumHeightStickLength)
                {
                    throw new ArgumentException($"{ nameof(size) } parameters should be greater than or equal to 2.");
                }

                // HeightScale

                if (MathUtil.IsZero(heightScale))
                {
                    throw new ArgumentException($"{ nameof(heightScale) } is 0.");
                }

                if ((heightType == HeightfieldTypes.Float) && !MathUtil.IsOne(heightScale))
                {
                    throw new ArgumentException($"{ nameof(heightScale) } should be 1 in { heightType } type.");
                }

                // HeightRange

                if (heightRange.Y < heightRange.X)
                {
                    throw new ArgumentException($"{ nameof(heightRange) }.{ nameof(heightRange.Y) } should be greater than { nameof(heightRange.X) }.");
                }

                if (Math.Abs((heightRange.Y / heightScale) - (heightRange.X / heightScale)) < 1)
                {
                    throw new ArgumentException($"{ nameof(heightRange) } is too short.");
                }

                if ((heightType == HeightfieldTypes.Byte) &&
                    (Math.Sign(heightRange.X) + Math.Sign(heightRange.Y)) == 0)
                {
                    throw new ArgumentException($"Can't handle { nameof(heightRange) } included both of positive and negative in { heightType } type.");
                }
            }
            catch (ArgumentException)
            {
                if (throwExceptionWhenInvalid) throw;

                return false;
            }

            return true;
        }

        public static float ConvertToFloatHeight(float minValue, float maxValue, float value) => MathUtil.InverseLerp(minValue, maxValue, MathUtil.Clamp(value, minValue, maxValue)) * 2 - 1;
        public static short ConvertToShortHeight(float minValue, float maxValue, float value) => (short)Math.Round(ConvertToFloatHeight(minValue, maxValue, value) * short.MaxValue, MidpointRounding.AwayFromZero);
        public static byte ConvertToByteHeight(float minValue, float maxValue, float value) => (byte)Math.Round(MathUtil.InverseLerp(minValue, maxValue, MathUtil.Clamp(value, minValue, maxValue)) * byte.MaxValue, MidpointRounding.AwayFromZero);

        public static float[] ConvertToFloatHeights(float[] values, float minValue, float maxValue) => values.Select((v) => ConvertToFloatHeight(minValue, maxValue, v)).ToArray();
        public static float[] ConvertToFloatHeights(short[] values, short minValue = short.MinValue, short maxValue = short.MaxValue) => values.Select((v) => ConvertToFloatHeight(minValue, maxValue, v)).ToArray();
        public static float[] ConvertToFloatHeights(byte[] values, byte minValue = byte.MinValue, byte maxValue = byte.MaxValue) => values.Select((v) => ConvertToFloatHeight(minValue, maxValue, v)).ToArray();

        public static short[] ConvertToShortHeights(float[] values, float minValue, float maxValue) => values.Select((v) => ConvertToShortHeight(minValue, maxValue, v)).ToArray();
        public static short[] ConvertToShortHeights(short[] values, short minValue = short.MinValue, short maxValue = short.MaxValue) => values.Select((v) => ConvertToShortHeight(minValue, maxValue, v)).ToArray();
        public static short[] ConvertToShortHeights(byte[] values, byte minValue = byte.MinValue, byte maxValue = byte.MaxValue) => values.Select((v) => ConvertToShortHeight(minValue, maxValue, v)).ToArray();

        public static byte[] ConvertToByteHeights(float[] values, float minValue, float maxValue) => values.Select((v) => ConvertToByteHeight(minValue, maxValue, v)).ToArray();
        public static byte[] ConvertToByteHeights(short[] values, short minValue = short.MinValue, short maxValue = short.MaxValue) => values.Select((v) => ConvertToByteHeight(minValue, maxValue, v)).ToArray();

        public static T[] Resize<T>(T[] pixels, Int2 originalSize, Int2 newSize)
        {
            if (originalSize.X < 1 || originalSize.Y < 1) throw new ArgumentException($"{ nameof(originalSize) }.{ nameof(originalSize.X) } and { nameof(originalSize.Y) } should be greater than 0.");
            if (newSize.X < 1 || newSize.Y < 1) throw new ArgumentException($"{ nameof(newSize) }.{ nameof(newSize.X) } and { nameof(newSize.Y) } should be greater than 0.");

            if (originalSize.Equals(newSize))
            {
                return pixels.ToArray();
            }

            var originalWidth = originalSize.X;
            var originalLength = originalSize.Y;
            var newWidth = newSize.X;
            var newLength = newSize.Y;

            var scaleX = (newWidth - 1) == 0 ? 0d : ((double)(originalWidth - 1)) / (newWidth - 1);
            var scaleY = (newLength - 1) == 0 ? 0d : ((double)(originalLength - 1)) / (newLength - 1);

            int GetOriginalIndex(int x, int y) =>
                (int)Math.Round(y * scaleY, MidpointRounding.AwayFromZero) * originalWidth +
                (int)Math.Round(x * scaleX, MidpointRounding.AwayFromZero);

            T[] newPixels = new T[newWidth * newLength];
            for (int y = 0; y < newLength; ++y)
            {
                for (int x = 0; x < newWidth; ++x)
                {
                    newPixels[y * newWidth + x] = pixels[GetOriginalIndex(x, y)];
                }
            }

            return newPixels;
        }
    }
}
