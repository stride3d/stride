using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Rendering.Rendering.MeshDecimator.Math
{
    internal struct SymmetricMatrix
    {
        #region Fields
        /// <summary>
        /// The m11 component.
        /// </summary>
        public double m0;
        /// <summary>
        /// The m12 component.
        /// </summary>
        public double m1;
        /// <summary>
        /// The m13 component.
        /// </summary>
        public double m2;
        /// <summary>
        /// The m14 component.
        /// </summary>
        public double m3;
        /// <summary>
        /// The m22 component.
        /// </summary>
        public double m4;
        /// <summary>
        /// The m23 component.
        /// </summary>
        public double m5;
        /// <summary>
        /// The m24 component.
        /// </summary>
        public double m6;
        /// <summary>
        /// The m33 component.
        /// </summary>
        public double m7;
        /// <summary>
        /// The m34 component.
        /// </summary>
        public double m8;
        /// <summary>
        /// The m44 component.
        /// </summary>
        public double m9;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the component value with a specific index.
        /// </summary>
        /// <param name="index">The component index.</param>
        /// <returns>The value.</returns>
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return m0;
                    case 1:
                        return m1;
                    case 2:
                        return m2;
                    case 3:
                        return m3;
                    case 4:
                        return m4;
                    case 5:
                        return m5;
                    case 6:
                        return m6;
                    case 7:
                        return m7;
                    case 8:
                        return m8;
                    case 9:
                        return m9;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a symmetric matrix with a value in each component.
        /// </summary>
        /// <param name="c">The component value.</param>
        public SymmetricMatrix(double c)
        {
            this.m0 = c;
            this.m1 = c;
            this.m2 = c;
            this.m3 = c;
            this.m4 = c;
            this.m5 = c;
            this.m6 = c;
            this.m7 = c;
            this.m8 = c;
            this.m9 = c;
        }

        /// <summary>
        /// Creates a symmetric matrix.
        /// </summary>
        /// <param name="m0">The m11 component.</param>
        /// <param name="m1">The m12 component.</param>
        /// <param name="m2">The m13 component.</param>
        /// <param name="m3">The m14 component.</param>
        /// <param name="m4">The m22 component.</param>
        /// <param name="m5">The m23 component.</param>
        /// <param name="m6">The m24 component.</param>
        /// <param name="m7">The m33 component.</param>
        /// <param name="m8">The m34 component.</param>
        /// <param name="m9">The m44 component.</param>
        public SymmetricMatrix(double m0, double m1, double m2, double m3,
            double m4, double m5, double m6, double m7, double m8, double m9)
        {
            this.m0 = m0;
            this.m1 = m1;
            this.m2 = m2;
            this.m3 = m3;
            this.m4 = m4;
            this.m5 = m5;
            this.m6 = m6;
            this.m7 = m7;
            this.m8 = m8;
            this.m9 = m9;
        }

        /// <summary>
        /// Creates a symmetric matrix from a plane.
        /// </summary>
        /// <param name="a">The plane x-component.</param>
        /// <param name="b">The plane y-component</param>
        /// <param name="c">The plane z-component</param>
        /// <param name="d">The plane w-component</param>
        public SymmetricMatrix(double a, double b, double c, double d)
        {
            this.m0 = a * a;
            this.m1 = a * b;
            this.m2 = a * c;
            this.m3 = a * d;

            this.m4 = b * b;
            this.m5 = b * c;
            this.m6 = b * d;

            this.m7 = c * c;
            this.m8 = c * d;

            this.m9 = d * d;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Adds two matrixes together.
        /// </summary>
        /// <param name="a">The left hand side.</param>
        /// <param name="b">The right hand side.</param>
        /// <returns>The resulting matrix.</returns>
        public static SymmetricMatrix operator +(SymmetricMatrix a, SymmetricMatrix b)
        {
            return new SymmetricMatrix(
                a.m0 + b.m0, a.m1 + b.m1, a.m2 + b.m2, a.m3 + b.m3,
                a.m4 + b.m4, a.m5 + b.m5, a.m6 + b.m6,
                a.m7 + b.m7, a.m8 + b.m8,
                a.m9 + b.m9
            );
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Determinant(0, 1, 2, 1, 4, 5, 2, 5, 7)
        /// </summary>
        /// <returns></returns>
        internal double Determinant1()
        {
            double det =
                m0 * m4 * m7 +
                m2 * m1 * m5 +
                m1 * m5 * m2 -
                m2 * m4 * m2 -
                m0 * m5 * m5 -
                m1 * m1 * m7;
            return det;
        }

        /// <summary>
        /// Determinant(1, 2, 3, 4, 5, 6, 5, 7, 8)
        /// </summary>
        /// <returns></returns>
        internal double Determinant2()
        {
            double det =
                m1 * m5 * m8 +
                m3 * m4 * m7 +
                m2 * m6 * m5 -
                m3 * m5 * m5 -
                m1 * m6 * m7 -
                m2 * m4 * m8;
            return det;
        }

        /// <summary>
        /// Determinant(0, 2, 3, 1, 5, 6, 2, 7, 8)
        /// </summary>
        /// <returns></returns>
        internal double Determinant3()
        {
            double det =
                m0 * m5 * m8 +
                m3 * m1 * m7 +
                m2 * m6 * m2 -
                m3 * m5 * m2 -
                m0 * m6 * m7 -
                m2 * m1 * m8;
            return det;
        }

        /// <summary>
        /// Determinant(0, 1, 3, 1, 4, 6, 2, 5, 8)
        /// </summary>
        /// <returns></returns>
        internal double Determinant4()
        {
            double det =
                m0 * m4 * m8 +
                m3 * m1 * m5 +
                m1 * m6 * m2 -
                m3 * m4 * m2 -
                m0 * m6 * m5 -
                m1 * m1 * m8;
            return det;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Computes the determinant of this matrix.
        /// </summary>
        /// <param name="a11">The a11 index.</param>
        /// <param name="a12">The a12 index.</param>
        /// <param name="a13">The a13 index.</param>
        /// <param name="a21">The a21 index.</param>
        /// <param name="a22">The a22 index.</param>
        /// <param name="a23">The a23 index.</param>
        /// <param name="a31">The a31 index.</param>
        /// <param name="a32">The a32 index.</param>
        /// <param name="a33">The a33 index.</param>
        /// <returns>The determinant value.</returns>
        public double Determinant(int a11, int a12, int a13,
            int a21, int a22, int a23,
            int a31, int a32, int a33)
        {
            double det =
                this[a11] * this[a22] * this[a33] +
                this[a13] * this[a21] * this[a32] +
                this[a12] * this[a23] * this[a31] -
                this[a13] * this[a22] * this[a31] -
                this[a11] * this[a23] * this[a32] -
                this[a12] * this[a21] * this[a33];
            return det;
        }
        #endregion
    }
}
