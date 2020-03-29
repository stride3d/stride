// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Runtime.CompilerServices;

namespace Xenko.Core.Mathematics
{
    /// <summary>
    /// Common utility methods for math operations.
    /// </summary>
    public static class MathUtil
    {
        /// <summary>
        /// The value for which all absolute numbers smaller than are considered equal to zero.
        /// </summary>
        public const float ZeroTolerance = 1e-6f; // Value a 8x higher than 1.19209290E-07F

        /// <summary>
        /// The value for which all absolute numbers smaller than are considered equal to zero.
        /// </summary>
        public const double ZeroToleranceDouble = double.Epsilon * 8;

        /// <summary>
        /// A value specifying the approximation of π which is 180 degrees.
        /// </summary>
        public const float Pi = (float)Math.PI;

        /// <summary>
        /// A value specifying the approximation of 2π which is 360 degrees.
        /// </summary>
        public const float TwoPi = (float)(2 * Math.PI);

        /// <summary>
        /// A value specifying the approximation of π/2 which is 90 degrees.
        /// </summary>
        public const float PiOverTwo = (float)(Math.PI / 2);

        /// <summary>
        /// A value specifying the approximation of π/4 which is 45 degrees.
        /// </summary>
        public const float PiOverFour = (float)(Math.PI / 4);

        /// <summary>
        /// Checks if a and b are almost equals, taking into account the magnitude of floating point numbers (unlike <see cref="WithinEpsilon"/> method). See Remarks.
        /// See remarks.
        /// </summary>
        /// <param name="a">The left value to compare.</param>
        /// <param name="b">The right value to compare.</param>
        /// <returns><c>true</c> if a almost equal to b, <c>false</c> otherwise</returns>
        /// <remarks>
        /// The code is using the technique described by Bruce Dawson in 
        /// <a href="http://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/">Comparing Floating point numbers 2012 edition</a>. 
        /// </remarks>
        public static unsafe bool NearEqual(float a, float b)
        {
            // Check if the numbers are really close -- needed
            // when comparing numbers near zero.
            if (IsZero(a - b))
                return true;

            // Original from Bruce Dawson: http://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
            int aInt = *(int*)&a;
            int bInt = *(int*)&b;

            // Different signs means they do not match.
            if ((aInt < 0) != (bInt < 0))
                return false;

            // Find the difference in ULPs.
            int ulp = Math.Abs(aInt - bInt);

            // Choose of maxUlp = 4
            // according to http://code.google.com/p/googletest/source/browse/trunk/include/gtest/internal/gtest-internal.h
            const int maxUlp = 4;
            return (ulp <= maxUlp);
        }

        /// <summary>
        /// Determines whether the specified value is close to zero (0.0f).
        /// </summary>
        /// <param name="a">The floating value.</param>
        /// <returns><c>true</c> if the specified value is close to zero (0.0f); otherwise, <c>false</c>.</returns>
        public static bool IsZero(float a)
        {
            return Math.Abs(a) < ZeroTolerance;
        }

        /// <summary>
        /// Determines whether the specified value is close to zero (0.0f).
        /// </summary>
        /// <param name="a">The floating value.</param>
        /// <returns><c>true</c> if the specified value is close to zero (0.0f); otherwise, <c>false</c>.</returns>
        public static bool IsZero(double a)
        {
            return Math.Abs(a) < ZeroToleranceDouble;
        }

        /// <summary>
        /// Determines whether the specified value is close to one (1.0f).
        /// </summary>
        /// <param name="a">The floating value.</param>
        /// <returns><c>true</c> if the specified value is close to one (1.0f); otherwise, <c>false</c>.</returns>
        public static bool IsOne(float a)
        {
            return IsZero(a - 1.0f);
        }

        /// <summary>
        /// Checks if a - b are almost equals within a float epsilon.
        /// </summary>
        /// <param name="a">The left value to compare.</param>
        /// <param name="b">The right value to compare.</param>
        /// <param name="epsilon">Epsilon value</param>
        /// <returns><c>true</c> if a almost equal to b within a float epsilon, <c>false</c> otherwise</returns>
        public static bool WithinEpsilon(float a, float b, float epsilon)
        {
            float num = a - b;
            return ((-epsilon <= num) && (num <= epsilon));
        }

        /// <summary>
        /// Creates a one-dimensional array of the specified <typeparamref name="T"/> and <paramref name="length"/> filled with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The Type of the array to create.</typeparam>
        /// <param name="value">The value to fill the array with.</param>
        /// <param name="length">The size of the array to create.</param>
        /// <returns>A new one-dimensional array of the specified type with the specified length and filled with the specified value.</returns>
        public static T[] Array<T>(T value, int length)
        {
            var result = new T[length];
            for (var i = 0; i < length; i++)
                result[i] = value;

            return result;
        }

        /// <summary>
        /// Converts revolutions to degrees.
        /// </summary>
        /// <param name="revolution">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RevolutionsToDegrees(float revolution)
        {
            return revolution * 360.0f;
        }

        /// <summary>
        /// Converts revolutions to radians.
        /// </summary>
        /// <param name="revolution">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RevolutionsToRadians(float revolution)
        {
            return revolution * TwoPi;
        }

        /// <summary>
        /// Converts revolutions to gradians.
        /// </summary>
        /// <param name="revolution">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RevolutionsToGradians(float revolution)
        {
            return revolution * 400.0f;
        }

        /// <summary>
        /// Converts degrees to revolutions.
        /// </summary>
        /// <param name="degree">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float DegreesToRevolutions(float degree)
        {
            return degree / 360.0f;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degree">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float DegreesToRadians(float degree)
        {
            return degree * (Pi / 180.0f);
        }

        /// <summary>
        /// Converts radians to revolutions.
        /// </summary>
        /// <param name="radian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RadiansToRevolutions(float radian)
        {
            return radian / TwoPi;
        }

        /// <summary>
        /// Converts radians to gradians.
        /// </summary>
        /// <param name="radian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RadiansToGradians(float radian)
        {
            return radian * (200.0f / Pi);
        }

        /// <summary>
        /// Converts gradians to revolutions.
        /// </summary>
        /// <param name="gradian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float GradiansToRevolutions(float gradian)
        {
            return gradian / 400.0f;
        }

        /// <summary>
        /// Converts gradians to degrees.
        /// </summary>
        /// <param name="gradian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float GradiansToDegrees(float gradian)
        {
            return gradian * (9.0f / 10.0f);
        }

        /// <summary>
        /// Converts gradians to radians.
        /// </summary>
        /// <param name="gradian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float GradiansToRadians(float gradian)
        {
            return gradian * (Pi / 200.0f);
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radian">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static float RadiansToDegrees(float radian)
        {
            return radian * (180.0f / Pi);
        }

        /// <summary>
        /// Clamps the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <returns>The result of clamping a value between min and max</returns>
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        /// <summary>
        /// Clamps the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <returns>The result of clamping a value between min and max</returns>
        public static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }

        /// <summary>
        /// Clamps the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <returns>The result of clamping a value between min and max</returns>
        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        /// <summary>
        /// Inverse-interpolates a value linearly.
        /// </summary>
        /// <param name="min">Minimum value that takes place in inverse-interpolation.</param>
        /// <param name="max">Maximum value that takes place in inverse-interpolation.</param>
        /// <param name="value">Value to get inverse interpolation.</param>
        /// <returns>Returns an inverse-linearly interpolated coeficient.</returns>
        public static float InverseLerp(float min, float max, float value)
        {
            if (IsZero(Math.Abs(max - min)))
                return float.NaN;
            return (value - min) / (max - min);
        }

        /// <summary>
        /// Inverse-interpolates a value linearly.
        /// </summary>
        /// <param name="min">Minimum value that takes place in inverse-interpolation.</param>
        /// <param name="max">Maximum value that takes place in inverse-interpolation.</param>
        /// <param name="value">Value to get inverse interpolation.</param>
        /// <returns>Returns an inverse-linearly interpolated coeficient.</returns>
        public static double InverseLerp(double min, double max, double value)
        {
            if (IsZero(Math.Abs(max - min)))
                return double.NaN;
            return (value - min) / (max - min);
        }

        /// <summary>
        /// Interpolates between two values using a linear function by a given amount.
        /// </summary>
        /// <remarks>
        /// See http://www.encyclopediaofmath.org/index.php/Linear_interpolation and
        /// http://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
        /// </remarks>
        /// <param name="from">Value to interpolate from.</param>
        /// <param name="to">Value to interpolate to.</param>
        /// <param name="amount">Interpolation amount.</param>
        /// <returns>The result of linear interpolation of values based on the amount.</returns>
        public static double Lerp(double from, double to, double amount)
        {
            return (1 - amount) * from + amount * to;
        }

        /// <summary>
        /// Interpolates between two values using a linear function by a given amount.
        /// </summary>
        /// <remarks>
        /// See http://www.encyclopediaofmath.org/index.php/Linear_interpolation and
        /// http://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
        /// </remarks>
        /// <param name="from">Value to interpolate from.</param>
        /// <param name="to">Value to interpolate to.</param>
        /// <param name="amount">Interpolation amount.</param>
        /// <returns>The result of linear interpolation of values based on the amount.</returns>
        public static float Lerp(float from, float to, float amount)
        {
            return (1 - amount) * from + amount * to;
        }

        /// <summary>
        /// Interpolates between two values using a linear function by a given amount.
        /// </summary>
        /// <remarks>
        /// See http://www.encyclopediaofmath.org/index.php/Linear_interpolation and
        /// http://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
        /// </remarks>
        /// <param name="from">Value to interpolate from.</param>
        /// <param name="to">Value to interpolate to.</param>
        /// <param name="amount">Interpolation amount.</param>
        /// <returns>The result of linear interpolation of values based on the amount.</returns>
        public static byte Lerp(byte from, byte to, float amount)
        {
            return (byte)Lerp((float)from, (float)to, amount);
        }

        /// <summary>
        /// Performs smooth (cubic Hermite) interpolation between 0 and 1.
        /// </summary>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Smoothstep
        /// </remarks>
        /// <param name="amount">Value between 0 and 1 indicating interpolation amount.</param>
        public static float SmoothStep(float amount)
        {
            return (amount <= 0) ? 0
                : (amount >= 1) ? 1
                : amount * amount * (3 - (2 * amount));
        }

        /// <summary>
        /// Performs a smooth(er) interpolation between 0 and 1 with 1st and 2nd order derivatives of zero at endpoints.
        /// </summary>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Smoothstep
        /// </remarks>
        /// <param name="amount">Value between 0 and 1 indicating interpolation amount.</param>
        public static float SmootherStep(float amount)
        {
            return (amount <= 0) ? 0
                : (amount >= 1) ? 1
                : amount * amount * amount * (amount * ((amount * 6) - 15) + 10);
        }

        /// <summary>
        /// Determines whether the value is inside the given range (inclusively).
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        /// <returns><c>true</c> if value is inside the specified range; otherwise, <c>false</c>.</returns>
        public static bool IsInRange(float value, float min, float max)
        {
            return min <= value && value <= max;
        }

        /// <summary>
        /// Determines whether the value is inside the given range (inclusively).
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        /// <returns><c>true</c> if value is inside the specified range; otherwise, <c>false</c>.</returns>
        public static bool IsInRange(int value, int min, int max)
        {
            return min <= value && value <= max;
        }

        /// <summary>
        /// Determines whether the specified x is pow2.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns><c>true</c> if the specified x is pow2; otherwise, <c>false</c>.</returns>
        public static bool IsPow2(int x)
        {
            return ((x != 0) && (x & (x - 1)) == 0);
        }

        /// <summary>
        /// Converts a float value from sRGB to linear.
        /// </summary>
        /// <param name="sRgbValue">The sRGB value.</param>
        /// <returns>A linear value.</returns>
        public static float SRgbToLinear(float sRgbValue)
        {
            if (sRgbValue < 0.04045f) return sRgbValue / 12.92f;
            return (float)Math.Pow((sRgbValue + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// Converts a float value from linear to sRGB.
        /// </summary>
        /// <param name="linearValue">The linear value.</param>
        /// <returns>The encoded sRGB value.</returns>
        public static float LinearToSRgb(float linearValue)
        {
            if (linearValue < 0.0031308f) return linearValue * 12.92f;
            return (float)(1.055 * Math.Pow(linearValue, 1 / 2.4) - 0.055);
        }

        /// <summary>
        /// Calculate the logarithm 2 of a floating point.
        /// </summary>
        /// <param name="x">The input float</param>
        /// <returns><value>Log2(x)</value></returns>
        public static float Log2(float x)
        {
            return (float)Math.Log(x) / 0.6931471805599453f;
        }

        /// <summary>
        /// Calculate the logarithm 2 of an integer.
        /// </summary>
        /// <param name="i">The input integer</param>
        /// <returns><value>the log2(i) rounded to lower integer</value></returns>
        public static int Log2(int i)
        {
            var r = 0;

            while ((i >>= 1) != 0)
                ++r;

            return r;
        }

        /// <summary>
        /// Get the next power of two of an integer.
        /// </summary>
        /// <param name="x">The size.</param>
        /// <returns>System.Int32.</returns>
        /// <remarks>https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2</remarks>
        public static int NextPowerOfTwo(int x)
        {
            if (x < 0)
                return 0;

            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        /// <summary>
        /// Get the next power of two for a size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>System.Int32.</returns>
        public static float NextPowerOfTwo(float size)
        {
            return (float)Math.Pow(2, Math.Ceiling(Math.Log(size, 2)));
        }

        /// <summary>
        /// Get the previous power of two of the provided integer.
        /// </summary>
        /// <param name="size">The value</param>
        public static int PreviousPowerOfTwo(int size)
        {
            return 1 << (int)Math.Floor(Math.Log(size, 2));
        }

        /// <summary>
        /// Get the previous power of two of the provided float.
        /// </summary>
        /// <param name="size">The value</param>
        public static float PreviousPowerOfTwo(float size)
        {
            return (float)Math.Pow(2, Math.Floor(Math.Log(size, 2)));
        }

        /// <summary>
        /// Alignes value up to match desire alignment.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="alignment">The alignment.</param>
        /// <returns>Aligned value (multiple of alignment).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignUp(int value, int alignment)
        {
            int mask = alignment - 1;
            return (value + mask) & ~mask;
        }

        /// <summary>
        /// Alignes value down to match desire alignment.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="alignment">The alignment.</param>
        /// <returns>Aligned value (multiple of alignment).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignDown(int value, int alignment)
        {
            int mask = alignment - 1;
            return value & ~mask;
        }

        /// <summary>
        /// Determines whether the specified value is aligned.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="alignment">The alignment.</param>
        /// <returns><c>true</c> if the specified value is aligned; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAligned(int value, int alignment)
        {
            return (value & (alignment - 1)) == 0;
        }

        /// <summary>
        /// Snaps a value to the nearest interval.
        /// </summary>
        /// <param name="value">The value to snap.</param>
        /// <param name="gap">The interval gap.</param>
        /// <returns>The nearest interval to the provided value.</returns>
        public static float Snap(float value, float gap)
        {
            if (gap == 0)
                return value;
            return (float)Math.Round((value / gap), MidpointRounding.AwayFromZero) * gap;
        }

        /// <summary>
        /// Snaps a value to the nearest interval.
        /// </summary>
        /// <param name="value">The value to snap.</param>
        /// <param name="gap">The interval gap.</param>
        /// <returns>The nearest interval to the provided value.</returns>
        public static double Snap(double value, double gap)
        {
            if (gap == 0)
                return value;
            return Math.Round((value / gap), MidpointRounding.AwayFromZero) * gap;
        }

        /// <summary>
        /// Snaps all vector components to the nearest interval.
        /// </summary>
        /// <param name="value">The vector to snap.</param>
        /// <param name="gap">The interval gap.</param>
        /// <returns>A vector which components are snapped to the nearest interval.</returns>
        public static Vector2 Snap(Vector2 value, float gap)
        {
            if (gap == 0)
                return value;
            return new Vector2(
                (float)Math.Round((value.X / gap), MidpointRounding.AwayFromZero) * gap,
                (float)Math.Round((value.Y / gap), MidpointRounding.AwayFromZero) * gap);
        }

        /// <summary>
        /// Snaps all vector components to the nearest interval.
        /// </summary>
        /// <param name="value">The vector to snap.</param>
        /// <param name="gap">The interval gap.</param>
        /// <returns>A vector which components are snapped to the nearest interval.</returns>
        public static Vector3 Snap(Vector3 value, float gap)
        {
            if (gap == 0)
                return value;
            return new Vector3(
                (float)Math.Round((value.X / gap), MidpointRounding.AwayFromZero) * gap,
                (float)Math.Round((value.Y / gap), MidpointRounding.AwayFromZero) * gap,
                (float)Math.Round((value.Z / gap), MidpointRounding.AwayFromZero) * gap);
        }

        /// <summary>
        /// Snaps all vector components to the nearest interval.
        /// </summary>
        /// <param name="value">The vector to snap.</param>
        /// <param name="gap">The interval gap.</param>
        /// <returns>A vector which components are snapped to the nearest interval.</returns>
        public static Vector4 Snap(Vector4 value, float gap)
        {
            if (gap == 0)
                return value;
            return new Vector4(
                (float)Math.Round((value.X / gap), MidpointRounding.AwayFromZero) * gap,
                (float)Math.Round((value.Y / gap), MidpointRounding.AwayFromZero) * gap,
                (float)Math.Round((value.Z / gap), MidpointRounding.AwayFromZero) * gap,
                (float)Math.Round((value.W / gap), MidpointRounding.AwayFromZero) * gap);
        }

        /// <summary>
        /// Computes standard mathematical modulo (as opposed to remainder).
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="divisor">The divisor.</param>
        /// <returns>A value between 0 and divisor. The result will have the same sign as divisor.</returns>
        public static float Mod(float value, float divisor)
        {
            return ((value % divisor) + divisor) % divisor;
        }
    }
}
