// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1003 // Symbols must be spaced correctly
#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
#pragma warning disable SA1009 // Closing parenthesis must be spaced correctly
#pragma warning disable SA1010 // Opening square brackets must be spaced correctly
#pragma warning disable SA1025 // Code must not contain multiple whitespace in a row
#pragma warning disable SA1119 // Statement must not use unnecessary parenthesis
#pragma warning disable SA1402 // File may only contain a single class
using System;

namespace Xenko.Core.Mathematics
{
    /// <summary>
    /// A representation of a sphere of values via Spherical Harmonics (SH).
    /// </summary>
    /// <typeparam name="TDataType">The type of data contained by the sphere</typeparam>
    [DataContract("SphericalHarmonicsGeneric")]
    public abstract class SphericalHarmonics<TDataType>
    {
        /// <summary>
        /// The maximum order supported.
        /// </summary>
        public const int MaximumOrder = 5;
        
        private int order;

        /// <summary>
        /// The order of calculation of the spherical harmonic.
        /// </summary>
        [DataMember(0)]
        public int Order
        {
            get { return order; }
            internal set
            {
                if (order>5)
                    throw new NotSupportedException("Only orders inferior or equal to 5 are supported");
                
                order = Math.Max(1, value);
            }
        }

        /// <summary>
        /// Get the coefficients defining the spherical harmonics (the spherical coordinates x{l,m} multiplying the spherical base Y{l,m}).
        /// </summary>
        [DataMember(1)]
        public TDataType[] Coefficients { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SphericalHarmonics{TDataType}"/> class (null, for serialization).
        /// </summary>
        internal SphericalHarmonics()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SphericalHarmonics{TDataType}"/> class.
        /// </summary>
        /// <param name="order">The order of the harmonics</param>
        protected SphericalHarmonics(int order)
        {
            this.order = order;
            Coefficients = new TDataType[order * order];
        }

        /// <summary>
        /// Evaluate the value of the spherical harmonics in the provided direction.
        /// </summary>
        /// <param name="direction">The direction</param>
        /// <returns>The value of the spherical harmonics in the direction</returns>
        public abstract TDataType Evaluate(Vector3 direction);

        /// <summary>
        /// Returns the coefficient x{l,m} of the spherical harmonics (the {l,m} spherical coordinate corresponding to the spherical base Y{l,m}).
        /// </summary>
        /// <param name="l">the l index of the coefficient</param>
        /// <param name="m">the m index of the coefficient</param>
        /// <returns>the value of the coefficient</returns>
        public TDataType this[int l, int m]
        {
            get
            {
                CheckIndicesValidity(l, m, order);
                return Coefficients[LmToCoefficientIndex(l, m)];
            }
            set
            {
                CheckIndicesValidity(l, m, order); 
                Coefficients[LmToCoefficientIndex(l, m)] = value;
            }
        }

        // ReSharper disable UnusedParameter.Local
        private static void CheckIndicesValidity(int l, int m, int maxOrder)
        // ReSharper restore UnusedParameter.Local
        {
            if (l > maxOrder - 1)
                throw new IndexOutOfRangeException("'l' parameter should be between '0' and '{0}' (order-1).".ToFormat(maxOrder-1));

            if (Math.Abs(m) > l)
                throw new IndexOutOfRangeException("'m' parameter should be between '-l' and '+l'.");
        }

        private static int LmToCoefficientIndex(int l, int m)
        {
            return l * l + l + m;
        }
    }

    /// <summary>
    /// A spherical harmonics representation of a cubemap.
    /// </summary>
    [DataContract("SphericalHarmonics")]
    public class SphericalHarmonics : SphericalHarmonics<Color3>
    {
        private readonly float[] baseValues;

        private const float Pi4 = 4 * MathUtil.Pi;
        private const float Pi16 = 16 * MathUtil.Pi;
        private const float Pi64 = 64 * MathUtil.Pi;
        private static readonly float SqrtPi = (float)Math.Sqrt(MathUtil.Pi);

        /// <summary>
        /// Base coefficients for SH.
        /// </summary>
        public static readonly float[] BaseCoefficients =
        {
            (float)(1.0/(2.0*SqrtPi)),

            (float)(-Math.Sqrt(3.0/Pi4)),
            (float)(Math.Sqrt(3.0/Pi4)),
            (float)(-Math.Sqrt(3.0/Pi4)),

            (float)(Math.Sqrt(15.0/Pi4)),
            (float)(-Math.Sqrt(15.0/Pi4)),
            (float)(Math.Sqrt(5.0/Pi16)),
            (float)(-Math.Sqrt(15.0/Pi4)),
            (float)(Math.Sqrt(15.0/Pi16)),

            -(float)Math.Sqrt(70/Pi64),
            (float)Math.Sqrt(105/Pi4),
            -(float)Math.Sqrt(42/Pi64),
            (float)Math.Sqrt(7/Pi16),
            -(float)Math.Sqrt(42/Pi64),
            (float)Math.Sqrt(105/Pi16),
            -(float)Math.Sqrt(70/Pi64),

            3*(float)Math.Sqrt(35/Pi16),
            -3*(float)Math.Sqrt(70/Pi64),
            3*(float)Math.Sqrt(5/Pi16),
            -3*(float)Math.Sqrt(10/Pi64),
            (float)(1.0/(16.0*SqrtPi)),
            -3*(float)Math.Sqrt(10/Pi64),
            3*(float)Math.Sqrt(5/Pi64),
            -3*(float)Math.Sqrt(70/Pi64),
            3*(float)Math.Sqrt(35/(4*Pi64)),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="SphericalHarmonics"/> class (null, for serialization).
        /// </summary>
        internal SphericalHarmonics()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SphericalHarmonics"/> class.
        /// </summary>
        /// <param name="order">The order of the harmonics</param>
        public SphericalHarmonics(int order)
            : base(order)
        {
            baseValues = new float[order * order];
        }

        /// <summary>
        /// Evaluates the color for the specified direction.
        /// </summary>
        /// <param name="direction">The direction to evaluate.</param>
        /// <returns>The color computed for this direction.</returns>
        public override Color3 Evaluate(Vector3 direction)
        {
            var x = direction.X;
            var y = direction.Y;
            var z = direction.Z;

            var x2 = x*x;
            var y2 = y*y;
            var z2 = z*z;

            var z3 = (float)Math.Pow(z, 3.0);

            var x4 = (float)Math.Pow(x, 4.0);
            var y4 = (float)Math.Pow(y, 4.0);
            var z4 = (float)Math.Pow(z, 4.0);

            //Equations based on data from: http://ppsloan.org/publications/StupidSH36.pdf
            baseValues[ 0] =  1/(2*SqrtPi);

            if (Order > 1)
            {
                baseValues[ 1] = -(float)Math.Sqrt(3/Pi4)*y;
                baseValues[ 2] =  (float)Math.Sqrt(3/Pi4)*z;
                baseValues[ 3] = -(float)Math.Sqrt(3/Pi4)*x;
                
                if (Order > 2)
                {
                    baseValues[ 4] =  (float)Math.Sqrt(15/Pi4)*y*x;
                    baseValues[ 5] = -(float)Math.Sqrt(15/Pi4)*y*z;
                    baseValues[ 6] =  (float)Math.Sqrt(5/Pi16)*(3*z2-1);
                    baseValues[ 7] = -(float)Math.Sqrt(15/Pi4)*x*z;
                    baseValues[ 8] =  (float)Math.Sqrt(15/Pi16)*(x2-y2);
                
                    if (Order > 3)
                    {
                        baseValues[ 9] = -(float)Math.Sqrt( 70/Pi64)*y*(3*x2-y2);
                        baseValues[10] =  (float)Math.Sqrt(105/ Pi4)*y*x*z;
                        baseValues[11] = -(float)Math.Sqrt( 42/Pi64)*y*(-1+5*z2);
                        baseValues[12] =  (float)Math.Sqrt(  7/Pi16)*(5*z3-3*z);
                        baseValues[13] = -(float)Math.Sqrt( 42/Pi64)*x*(-1+5*z2);
                        baseValues[14] =  (float)Math.Sqrt(105/Pi16)*(x2-y2)*z;
                        baseValues[15] = -(float)Math.Sqrt( 70/Pi64)*x*(x2-3*y2);
                
                        if (Order > 4)
                        {
                            baseValues[16] =  3*(float)Math.Sqrt(35/Pi16)*x*y*(x2-y2);
                            baseValues[17] = -3*(float)Math.Sqrt(70/Pi64)*y*z*(3*x2-y2);
                            baseValues[18] =  3*(float)Math.Sqrt( 5/Pi16)*y*x*(-1+7*z2);
                            baseValues[19] = -3*(float)Math.Sqrt(10/Pi64)*y*z*(-3+7*z2);
                            baseValues[20] =  (105*z4-90*z2+9)/(16*SqrtPi);
                            baseValues[21] = -3*(float)Math.Sqrt(10/Pi64)*x*z*(-3+7*z2);
                            baseValues[22] =  3*(float)Math.Sqrt( 5/Pi64)*(x2-y2)*(-1+7*z2);
                            baseValues[23] = -3*(float)Math.Sqrt(70/Pi64)*x*z*(x2-3*y2);
                            baseValues[24] =  3*(float)Math.Sqrt(35/(4*Pi64))*(x4-6*y2*x2+y4);
                        }
                    }
                }
            }

            var data = new Color3();

            for (int i = 0; i < baseValues.Length; i++)
                data += Coefficients[i] * baseValues[i];

            return data;
        }
    }
}
