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
#pragma warning disable SA1107 // Code must not contain multiple statements on one line
#pragma warning disable SA1117 // Parameters must be on same line or separate lines
#pragma warning disable SA1313 // Parameter names must begin with lower-case letter
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Xenko.Core.Mathematics
{
    /// <summary>
    /// Represents a 4x4 mathematical matrix.
    /// </summary>
    [DataContract("float4x4")]
    [DataStyle(DataStyle.Compact)]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Matrix : IEquatable<Matrix>, IFormattable
    {
        /// <summary>
        /// The size of the <see cref="Xenko.Core.Mathematics.Matrix"/> type, in bytes.
        /// </summary>
        public static readonly int SizeInBytes = Utilities.SizeOf<Matrix>();

        /// <summary>
        /// A <see cref="Xenko.Core.Mathematics.Matrix"/> with all of its components set to zero.
        /// </summary>
        public static readonly Matrix Zero = new Matrix();

        /// <summary>
        /// The identity <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        public static readonly Matrix Identity = new Matrix() { M11 = 1.0f, M22 = 1.0f, M33 = 1.0f, M44 = 1.0f };

        /// <summary>
        /// Value at row 1 column 1 of the matrix.
        /// </summary>
        public float M11;

        /// <summary>
        /// Value at row 2 column 1 of the matrix.
        /// </summary>
        public float M21;

        /// <summary>
        /// Value at row 3 column 1 of the matrix.
        /// </summary>
        public float M31;

        /// <summary>
        /// Value at row 4 column 1 of the matrix.
        /// </summary>
        public float M41;

        /// <summary>
        /// Value at row 1 column 2 of the matrix.
        /// </summary>
        public float M12;

        /// <summary>
        /// Value at row 2 column 2 of the matrix.
        /// </summary>
        public float M22;

        /// <summary>
        /// Value at row 3 column 2 of the matrix.
        /// </summary>
        public float M32;

        /// <summary>
        /// Value at row 4 column 2 of the matrix.
        /// </summary>
        public float M42;

        /// <summary>
        /// Value at row 1 column 3 of the matrix.
        /// </summary>
        public float M13;

        /// <summary>
        /// Value at row 2 column 3 of the matrix.
        /// </summary>
        public float M23;

        /// <summary>
        /// Value at row 3 column 3 of the matrix.
        /// </summary>
        public float M33;

        /// <summary>
        /// Value at row 4 column 3 of the matrix.
        /// </summary>
        public float M43;

        /// <summary>
        /// Value at row 1 column 4 of the matrix.
        /// </summary>
        public float M14;

        /// <summary>
        /// Value at row 2 column 4 of the matrix.
        /// </summary>
        public float M24;

        /// <summary>
        /// Value at row 3 column 4 of the matrix.
        /// </summary>
        public float M34;

        /// <summary>
        /// Value at row 4 column 4 of the matrix.
        /// </summary>
        public float M44;

        /// <summary>
        /// Initializes a new instance of the <see cref="Xenko.Core.Mathematics.Matrix"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public Matrix(float value)
        {
            M11 = M21 = M31 = M41 =
            M12 = M22 = M32 = M42 =
            M13 = M23 = M33 = M43 =
            M14 = M24 = M34 = M44 = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Xenko.Core.Mathematics.Matrix"/> struct.
        /// </summary>
        /// <param name="M11">The value to assign at row 1 column 1 of the matrix.</param>
        /// <param name="M12">The value to assign at row 1 column 2 of the matrix.</param>
        /// <param name="M13">The value to assign at row 1 column 3 of the matrix.</param>
        /// <param name="M14">The value to assign at row 1 column 4 of the matrix.</param>
        /// <param name="M21">The value to assign at row 2 column 1 of the matrix.</param>
        /// <param name="M22">The value to assign at row 2 column 2 of the matrix.</param>
        /// <param name="M23">The value to assign at row 2 column 3 of the matrix.</param>
        /// <param name="M24">The value to assign at row 2 column 4 of the matrix.</param>
        /// <param name="M31">The value to assign at row 3 column 1 of the matrix.</param>
        /// <param name="M32">The value to assign at row 3 column 2 of the matrix.</param>
        /// <param name="M33">The value to assign at row 3 column 3 of the matrix.</param>
        /// <param name="M34">The value to assign at row 3 column 4 of the matrix.</param>
        /// <param name="M41">The value to assign at row 4 column 1 of the matrix.</param>
        /// <param name="M42">The value to assign at row 4 column 2 of the matrix.</param>
        /// <param name="M43">The value to assign at row 4 column 3 of the matrix.</param>
        /// <param name="M44">The value to assign at row 4 column 4 of the matrix.</param>
        public Matrix(float M11, float M12, float M13, float M14,
            float M21, float M22, float M23, float M24,
            float M31, float M32, float M33, float M34,
            float M41, float M42, float M43, float M44)
        {
            this.M11 = M11; this.M12 = M12; this.M13 = M13; this.M14 = M14;
            this.M21 = M21; this.M22 = M22; this.M23 = M23; this.M24 = M24;
            this.M31 = M31; this.M32 = M32; this.M33 = M33; this.M34 = M34;
            this.M41 = M41; this.M42 = M42; this.M43 = M43; this.M44 = M44;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Xenko.Core.Mathematics.Matrix"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the components of the matrix. This must be an array with sixteen elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than sixteen elements.</exception>
        public Matrix(float[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 16)
                throw new ArgumentOutOfRangeException("values", "There must be sixteen and only sixteen input values for Matrix.");

            M11 = values[0];
            M12 = values[1];
            M13 = values[2];
            M14 = values[3];

            M21 = values[4];
            M22 = values[5];
            M23 = values[6];
            M24 = values[7];

            M31 = values[8];
            M32 = values[9];
            M33 = values[10];
            M34 = values[11];

            M41 = values[12];
            M42 = values[13];
            M43 = values[14];
            M44 = values[15];
        }

        /// <summary>
        /// Gets or sets the first row in the matrix; that is M11, M12, M13, and M14.
        /// </summary>
        [DataMemberIgnore]
        public Vector4 Row1
        {
            get { return new Vector4(M11, M12, M13, M14); }
            set { M11 = value.X; M12 = value.Y; M13 = value.Z; M14 = value.W; }
        }

        /// <summary>
        /// Gets or sets the second row in the matrix; that is M21, M22, M23, and M24.
        /// </summary>
        [DataMemberIgnore]
        public Vector4 Row2
        {
            get { return new Vector4(M21, M22, M23, M24); }
            set { M21 = value.X; M22 = value.Y; M23 = value.Z; M24 = value.W; }
        }

        /// <summary>
        /// Gets or sets the third row in the matrix; that is M31, M32, M33, and M34.
        /// </summary>
        [DataMemberIgnore]
        public Vector4 Row3
        {
            get { return new Vector4(M31, M32, M33, M34); }
            set { M31 = value.X; M32 = value.Y; M33 = value.Z; M34 = value.W; }
        }

        /// <summary>
        /// Gets or sets the fourth row in the matrix; that is M41, M42, M43, and M44.
        /// </summary>
        [DataMemberIgnore]
        public Vector4 Row4
        {
            get { return new Vector4(M41, M42, M43, M44); }
            set { M41 = value.X; M42 = value.Y; M43 = value.Z; M44 = value.W; }
        }

        /// <summary>
        /// Gets or sets the first column in the matrix; that is M11, M21, M31, and M41.
        /// </summary>
        [DataMemberIgnore]
        public Vector4 Column1
        {
            get { return new Vector4(M11, M21, M31, M41); }
            set { M11 = value.X; M21 = value.Y; M31 = value.Z; M41 = value.W; }
        }

        /// <summary>
        /// Gets or sets the second column in the matrix; that is M12, M22, M32, and M42.
        /// </summary>
        [DataMemberIgnore]
        public Vector4 Column2
        {
            get { return new Vector4(M12, M22, M32, M42); }
            set { M12 = value.X; M22 = value.Y; M32 = value.Z; M42 = value.W; }
        }

        /// <summary>
        /// Gets or sets the third column in the matrix; that is M13, M23, M33, and M43.
        /// </summary>
        [DataMemberIgnore]
        public Vector4 Column3
        {
            get { return new Vector4(M13, M23, M33, M43); }
            set { M13 = value.X; M23 = value.Y; M33 = value.Z; M43 = value.W; }
        }

        /// <summary>
        /// Gets or sets the fourth column in the matrix; that is M14, M24, M34, and M44.
        /// </summary>
        [DataMemberIgnore]
        public Vector4 Column4
        {
            get { return new Vector4(M14, M24, M34, M44); }
            set { M14 = value.X; M24 = value.Y; M34 = value.Z; M44 = value.W; }
        }

        /// <summary>
        /// Gets or sets the translation of the matrix; that is M41, M42, and M43.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 TranslationVector
        {
            get { return new Vector3(M41, M42, M43); }
            set { M41 = value.X; M42 = value.Y; M43 = value.Z; }
        }

        /// <summary>
        /// Gets or sets the scale of the matrix; that is M11, M22, and M33.
        /// </summary>
        /// <remarks>This property does not do any computation and will return a correct scale vector only if the matrix is a scale matrix.</remarks>
        [DataMemberIgnore]
        public Vector3 ScaleVector
        {
            get { return new Vector3(M11, M22, M33); }
            set { M11 = value.X; M22 = value.Y; M33 = value.Z; }
        }

        /// <summary>
        /// Gets or sets the up <see cref="Vector3"/> of the matrix; that is M21, M22, and M23.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 Up
        {
            get { return new Vector3(M21, M22, M23); }
            set { M21 = value.X; M22 = value.Y; M23 = value.Z; }
        }

        /// <summary>
        /// Gets or sets the down <see cref="Vector3"/> of the matrix; that is -M21, -M22, and -M23.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 Down
        {
            get { return new Vector3(-M21, -M22, -M23); }
            set { M21 = -value.X; M22 = -value.Y; M23 = -value.Z; }
        }

        /// <summary>
        /// Gets or sets the right <see cref="Vector3"/> of the matrix; that is M11, M12, and M13.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 Right
        {
            get { return new Vector3(M11, M12, M13); }
            set { M11 = value.X; M12 = value.Y; M13 = value.Z; }
        }

        /// <summary>
        /// Gets or sets the left <see cref="Vector3"/> of the matrix; that is -M11, -M12, and -M13.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 Left
        {
            get { return new Vector3(-M11, -M12, -M13); }
            set { M11 = -value.X; M12 = -value.Y; M13 = -value.Z; }
        }

        /// <summary>
        /// Gets or sets the forward <see cref="Vector3"/> of the matrix; that is -M31, -M32, and -M33.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 Forward
        {
            get { return new Vector3(-M31, -M32, -M33); }
            set { M31 = -value.X; M32 = -value.Y; M33 = -value.Z; }
        }

        /// <summary>
        /// Gets or sets the backward <see cref="Vector3"/> of the matrix; that is M31, M32, and M33.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 Backward
        {
            get { return new Vector3(M31, M32, M33); }
            set { M31 = value.X; M32 = value.Y; M33 = value.Z; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is an identity matrix.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is an identity matrix; otherwise, <c>false</c>.
        /// </value>
        public bool IsIdentity
        {
            get { return this.Equals(Identity); }
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the matrix component, depending on the index.</value>
        /// <param name="index">The zero-based index of the component to access.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 15].</exception>
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return M11;
                    case 1: return M12;
                    case 2: return M13;
                    case 3: return M14;
                    case 4: return M21;
                    case 5: return M22;
                    case 6: return M23;
                    case 7: return M24;
                    case 8: return M31;
                    case 9: return M32;
                    case 10: return M33;
                    case 11: return M34;
                    case 12: return M41;
                    case 13: return M42;
                    case 14: return M43;
                    case 15: return M44;
                }

                throw new ArgumentOutOfRangeException("index", "Indices for Matrix run from 0 to 15, inclusive.");
            }

            set
            {
                switch (index)
                {
                    case 0: M11 = value; break;
                    case 1: M12 = value; break;
                    case 2: M13 = value; break;
                    case 3: M14 = value; break;
                    case 4: M21 = value; break;
                    case 5: M22 = value; break;
                    case 6: M23 = value; break;
                    case 7: M24 = value; break;
                    case 8: M31 = value; break;
                    case 9: M32 = value; break;
                    case 10: M33 = value; break;
                    case 11: M34 = value; break;
                    case 12: M41 = value; break;
                    case 13: M42 = value; break;
                    case 14: M43 = value; break;
                    case 15: M44 = value; break;
                    default: throw new ArgumentOutOfRangeException("index", "Indices for Matrix run from 0 to 15, inclusive.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the matrix component, depending on the index.</value>
        /// <param name="row">The row of the matrix to access.</param>
        /// <param name="column">The column of the matrix to access.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="row"/> or <paramref name="column"/>is out of the range [0, 3].</exception>
        public float this[int row, int column]
        {
            get
            {
                if (row < 0 || row > 3)
                    throw new ArgumentOutOfRangeException("row", "Rows and columns for matrices run from 0 to 3, inclusive.");
                if (column < 0 || column > 3)
                    throw new ArgumentOutOfRangeException("column", "Rows and columns for matrices run from 0 to 3, inclusive.");

                return this[(row * 4) + column];
            }

            set
            {
                if (row < 0 || row > 3)
                    throw new ArgumentOutOfRangeException("row", "Rows and columns for matrices run from 0 to 3, inclusive.");
                if (column < 0 || column > 3)
                    throw new ArgumentOutOfRangeException("column", "Rows and columns for matrices run from 0 to 3, inclusive.");

                this[(row * 4) + column] = value;
            }
        }

        /// <summary>
        /// Calculates the determinant of the matrix.
        /// </summary>
        /// <returns>The determinant of the matrix.</returns>
        public float Determinant()
        {
            float temp1 = (M33 * M44) - (M34 * M43);
            float temp2 = (M32 * M44) - (M34 * M42);
            float temp3 = (M32 * M43) - (M33 * M42);
            float temp4 = (M31 * M44) - (M34 * M41);
            float temp5 = (M31 * M43) - (M33 * M41);
            float temp6 = (M31 * M42) - (M32 * M41);

            return ((((M11 * (((M22 * temp1) - (M23 * temp2)) + (M24 * temp3))) - (M12 * (((M21 * temp1) -
                (M23 * temp4)) + (M24 * temp5)))) + (M13 * (((M21 * temp2) - (M22 * temp4)) + (M24 * temp6)))) -
                (M14 * (((M21 * temp3) - (M22 * temp5)) + (M23 * temp6))));
        }

        /// <summary>
        /// Inverts the matrix.
        /// </summary>
        public void Invert()
        {
            Invert(ref this, out this);
        }

        /// <summary>
        /// Transposes the matrix.
        /// </summary>
        public void Transpose()
        {
            float temp;

            temp = M21; M21 = M12; M12 = temp;
            temp = M31; M31 = M13; M13 = temp;
            temp = M41; M41 = M14; M14 = temp;

            temp = M32; M32 = M23; M23 = temp;
            temp = M42; M42 = M24; M24 = temp;

            temp = M43; M43 = M34; M34 = temp;
        }

        /// <summary>
        /// Orthogonalizes the specified matrix.
        /// </summary>
        /// <remarks>
        /// <para>Orthogonalization is the process of making all rows orthogonal to each other. This
        /// means that any given row in the matrix will be orthogonal to any other given row in the
        /// matrix.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting matrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the matrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public void Orthogonalize()
        {
            Orthogonalize(ref this, out this);
        }

        /// <summary>
        /// Orthonormalizes the specified matrix.
        /// </summary>
        /// <remarks>
        /// <para>Orthonormalization is the process of making all rows and columns orthogonal to each
        /// other and making all rows and columns of unit length. This means that any given row will
        /// be orthogonal to any other given row and any given column will be orthogonal to any other
        /// given column. Any given row will not be orthogonal to any given column. Every row and every
        /// column will be of unit length.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting matrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the matrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public void Orthonormalize()
        {
            Orthonormalize(ref this, out this);
        }

        /// <summary>
        /// Decomposes a matrix into an orthonormalized matrix Q and a right traingular matrix R.
        /// </summary>
        /// <param name="Q">When the method completes, contains the orthonormalized matrix of the decomposition.</param>
        /// <param name="R">When the method completes, contains the right triangular matrix of the decomposition.</param>
        public void DecomposeQR(out Matrix Q, out Matrix R)
        {
            Matrix temp = this;
            temp.Transpose();
            Orthonormalize(ref temp, out Q);
            Q.Transpose();

            R = new Matrix();
            R.M11 = Vector4.Dot(Q.Column1, Column1);
            R.M12 = Vector4.Dot(Q.Column1, Column2);
            R.M13 = Vector4.Dot(Q.Column1, Column3);
            R.M14 = Vector4.Dot(Q.Column1, Column4);

            R.M22 = Vector4.Dot(Q.Column2, Column2);
            R.M23 = Vector4.Dot(Q.Column2, Column3);
            R.M24 = Vector4.Dot(Q.Column2, Column4);

            R.M33 = Vector4.Dot(Q.Column3, Column3);
            R.M34 = Vector4.Dot(Q.Column3, Column4);

            R.M44 = Vector4.Dot(Q.Column4, Column4);
        }

        /// <summary>
        /// Decomposes a matrix into a lower triangular matrix L and an orthonormalized matrix Q.
        /// </summary>
        /// <param name="L">When the method completes, contains the lower triangular matrix of the decomposition.</param>
        /// <param name="Q">When the method completes, contains the orthonormalized matrix of the decomposition.</param>
        public void DecomposeLQ(out Matrix L, out Matrix Q)
        {
            Orthonormalize(ref this, out Q);

            L = new Matrix();
            L.M11 = Vector4.Dot(Q.Row1, Row1);

            L.M21 = Vector4.Dot(Q.Row1, Row2);
            L.M22 = Vector4.Dot(Q.Row2, Row2);

            L.M31 = Vector4.Dot(Q.Row1, Row3);
            L.M32 = Vector4.Dot(Q.Row2, Row3);
            L.M33 = Vector4.Dot(Q.Row3, Row3);

            L.M41 = Vector4.Dot(Q.Row1, Row4);
            L.M42 = Vector4.Dot(Q.Row2, Row4);
            L.M43 = Vector4.Dot(Q.Row3, Row4);
            L.M44 = Vector4.Dot(Q.Row4, Row4);
        }

        /// <summary>
        /// Decomposes a rotation matrix with the specified yaw, pitch, roll
        /// </summary>
        /// <param name="yaw">The yaw.</param>
        /// <param name="pitch">The pitch.</param>
        /// <param name="roll">The roll.</param>
        public void Decompose(out float yaw, out float pitch, out float roll)
        {
            pitch = (float)Math.Asin(-M32);
            // Hardcoded constant - burn him, he's a witch
            // double threshold = 0.001;
            double test = Math.Cos(pitch);
            if (test > MathUtil.ZeroTolerance)
            {
                roll = (float)Math.Atan2(M12, M22);
                yaw = (float)Math.Atan2(M31, M33);
            }
            else
            {
                roll = (float)Math.Atan2(-M21, M11);
                yaw = 0.0f;
            }
        }

        /// <summary>
        /// Decomposes a rotation matrix with the specified X, Y and Z euler angles.
        /// Matrix.RotationX(rotation.X) * Matrix.RotationY(rotation.Y) * Matrix.RotationZ(rotation.Z) should represent the same rotation.
        /// </summary>
        /// <param name="rotation">The vector containing the 3 rotations angles to be applied in order.</param>
        public void DecomposeXYZ(out Vector3 rotation)
        {
            rotation.Y = (float)Math.Asin(-M13);
            double test = Math.Cos(rotation.Y);
            if (test > 1e-6f)
            {
                rotation.Z = (float)Math.Atan2(M12, M11);
                rotation.X = (float)Math.Atan2(M23, M33);
            }
            else
            {
                rotation.Z = (float)Math.Atan2(-M21, M31);
                rotation.X = 0.0f;
            }
        }

        /// <summary>
        /// Decomposes a matrix into a scale, rotation, and translation.
        /// </summary>
        /// <param name="scale">When the method completes, contains the scaling component of the decomposed matrix.</param>
        /// <param name="translation">When the method completes, contains the translation component of the decomposed matrix.</param>
        /// <returns><c>true</c> if a rotation exist for this matrix, <c>false</c> otherwise.</returns>
        /// <remarks>This method is designed to decompose an SRT transformation matrix only.</remarks>
        public bool Decompose(out Vector3 scale, out Vector3 translation)
        {
            //Source: Unknown
            //References: http://www.gamedev.net/community/forums/topic.asp?topic_id=441695

            //Get the translation.
            translation.X = this.M41;
            translation.Y = this.M42;
            translation.Z = this.M43;

            //Scaling is the length of the rows.
            scale.X = (float)Math.Sqrt((M11 * M11) + (M12 * M12) + (M13 * M13));
            scale.Y = (float)Math.Sqrt((M21 * M21) + (M22 * M22) + (M23 * M23));
            scale.Z = (float)Math.Sqrt((M31 * M31) + (M32 * M32) + (M33 * M33));

            //If any of the scaling factors are zero, than the rotation matrix can not exist.
            if (Math.Abs(scale.X) < MathUtil.ZeroTolerance ||
                Math.Abs(scale.Y) < MathUtil.ZeroTolerance ||
                Math.Abs(scale.Z) < MathUtil.ZeroTolerance)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Decomposes a matrix into a scale, rotation, and translation.
        /// </summary>
        /// <param name="scale">When the method completes, contains the scaling component of the decomposed matrix.</param>
        /// <param name="rotation">When the method completes, contains the rtoation component of the decomposed matrix.</param>
        /// <param name="translation">When the method completes, contains the translation component of the decomposed matrix.</param>
        /// <remarks>
        /// This method is designed to decompose an SRT transformation matrix only.
        /// </remarks>
        public bool Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            Matrix rotationMatrix;
            Decompose(out scale, out rotationMatrix, out translation);
            Quaternion.RotationMatrix(ref rotationMatrix, out rotation);
            return true;
        }

        /// <summary>
        /// Decomposes a matrix into a scale, rotation, and translation.
        /// </summary>
        /// <param name="scale">When the method completes, contains the scaling component of the decomposed matrix.</param>
        /// <param name="rotation">When the method completes, contains the rtoation component of the decomposed matrix.</param>
        /// <param name="translation">When the method completes, contains the translation component of the decomposed matrix.</param>
        /// <remarks>
        /// This method is designed to decompose an SRT transformation matrix only.
        /// </remarks>
        public bool Decompose(out Vector3 scale, out Matrix rotation, out Vector3 translation)
        {
            //Source: Unknown
            //References: http://www.gamedev.net/community/forums/topic.asp?topic_id=441695

            //Get the translation.
            translation.X = this.M41;
            translation.Y = this.M42;
            translation.Z = this.M43;

            //Scaling is the length of the rows.
            scale.X = (float)Math.Sqrt((M11 * M11) + (M12 * M12) + (M13 * M13));
            scale.Y = (float)Math.Sqrt((M21 * M21) + (M22 * M22) + (M23 * M23));
            scale.Z = (float)Math.Sqrt((M31 * M31) + (M32 * M32) + (M33 * M33));

            //If any of the scaling factors are zero, than the rotation matrix can not exist.
            if (Math.Abs(scale.X) < MathUtil.ZeroTolerance ||
                Math.Abs(scale.Y) < MathUtil.ZeroTolerance ||
                Math.Abs(scale.Z) < MathUtil.ZeroTolerance)
            {
                rotation = Matrix.Identity;
                return false;
            }

            // Calculate an perfect orthonormal matrix (no reflections)
            var at = new Vector3(M31 / scale.Z, M32 / scale.Z, M33 / scale.Z);
            var up = Vector3.Cross(at, new Vector3(M11 / scale.X, M12 / scale.X, M13 / scale.X));
            var right = Vector3.Cross(up, at);

            rotation = Identity;
            rotation.Right = right;
            rotation.Up = up;
            rotation.Backward = at;

            // In case of reflexions
            scale.X = Vector3.Dot(right, Right) > 0.0f ? scale.X : -scale.X;
            scale.Y = Vector3.Dot(up, Up) > 0.0f ? scale.Y : -scale.Y;
            scale.Z = Vector3.Dot(at, Backward) > 0.0f ? scale.Z : -scale.Z;

            return true;
        }

        /// <summary>
        /// Exchanges two rows in the matrix.
        /// </summary>
        /// <param name="firstRow">The first row to exchange. This is an index of the row starting at zero.</param>
        /// <param name="secondRow">The second row to exchange. This is an index of the row starting at zero.</param>
        public void ExchangeRows(int firstRow, int secondRow)
        {
            if (firstRow < 0)
                throw new ArgumentOutOfRangeException("firstRow", "The parameter firstRow must be greater than or equal to zero.");
            if (firstRow > 3)
                throw new ArgumentOutOfRangeException("firstRow", "The parameter firstRow must be less than or equal to three.");
            if (secondRow < 0)
                throw new ArgumentOutOfRangeException("secondRow", "The parameter secondRow must be greater than or equal to zero.");
            if (secondRow > 3)
                throw new ArgumentOutOfRangeException("secondRow", "The parameter secondRow must be less than or equal to three.");

            if (firstRow == secondRow)
                return;

            float temp0 = this[secondRow, 0];
            float temp1 = this[secondRow, 1];
            float temp2 = this[secondRow, 2];
            float temp3 = this[secondRow, 3];

            this[secondRow, 0] = this[firstRow, 0];
            this[secondRow, 1] = this[firstRow, 1];
            this[secondRow, 2] = this[firstRow, 2];
            this[secondRow, 3] = this[firstRow, 3];

            this[firstRow, 0] = temp0;
            this[firstRow, 1] = temp1;
            this[firstRow, 2] = temp2;
            this[firstRow, 3] = temp3;
        }

        /// <summary>
        /// Exchange columns.
        /// </summary>
        /// <param name="firstColumn">The first column to exchange.</param>
        /// <param name="secondColumn">The second column to exchange.</param>
        public void ExchangeColumns(int firstColumn, int secondColumn)
        {
            if (firstColumn < 0)
                throw new ArgumentOutOfRangeException("firstColumn", "The parameter firstColumn must be greater than or equal to zero.");
            if (firstColumn > 3)
                throw new ArgumentOutOfRangeException("firstColumn", "The parameter firstColumn must be less than or equal to three.");
            if (secondColumn < 0)
                throw new ArgumentOutOfRangeException("secondColumn", "The parameter secondColumn must be greater than or equal to zero.");
            if (secondColumn > 3)
                throw new ArgumentOutOfRangeException("secondColumn", "The parameter secondColumn must be less than or equal to three.");

            if (firstColumn == secondColumn)
                return;

            float temp0 = this[0, secondColumn];
            float temp1 = this[1, secondColumn];
            float temp2 = this[2, secondColumn];
            float temp3 = this[3, secondColumn];

            this[0, secondColumn] = this[0, firstColumn];
            this[1, secondColumn] = this[1, firstColumn];
            this[2, secondColumn] = this[2, firstColumn];
            this[3, secondColumn] = this[3, firstColumn];

            this[0, firstColumn] = temp0;
            this[1, firstColumn] = temp1;
            this[2, firstColumn] = temp2;
            this[3, firstColumn] = temp3;
        }

        /// <summary>
        /// Creates an array containing the elements of the matrix.
        /// </summary>
        /// <returns>A sixteen-element array containing the components of the matrix.</returns>
        public float[] ToArray()
        {
            return new[] { M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44 };
        }

        /// <summary>
        /// Determines the sum of two matrices.
        /// </summary>
        /// <param name="left">The first matrix to add.</param>
        /// <param name="right">The second matrix to add.</param>
        /// <param name="result">When the method completes, contains the sum of the two matrices.</param>
        public static void Add(ref Matrix left, ref Matrix right, out Matrix result)
        {
            result.M11 = left.M11 + right.M11;
            result.M21 = left.M21 + right.M21;
            result.M31 = left.M31 + right.M31;
            result.M41 = left.M41 + right.M41;
            result.M12 = left.M12 + right.M12;
            result.M22 = left.M22 + right.M22;
            result.M32 = left.M32 + right.M32;
            result.M42 = left.M42 + right.M42;
            result.M13 = left.M13 + right.M13;
            result.M23 = left.M23 + right.M23;
            result.M33 = left.M33 + right.M33;
            result.M43 = left.M43 + right.M43;
            result.M14 = left.M14 + right.M14;
            result.M24 = left.M24 + right.M24;
            result.M34 = left.M34 + right.M34;
            result.M44 = left.M44 + right.M44;
        }

        /// <summary>
        /// Determines the sum of two matrices.
        /// </summary>
        /// <param name="left">The first matrix to add.</param>
        /// <param name="right">The second matrix to add.</param>
        /// <returns>The sum of the two matrices.</returns>
        public static Matrix Add(Matrix left, Matrix right)
        {
            Matrix result;
            Add(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Determines the difference between two matrices.
        /// </summary>
        /// <param name="left">The first matrix to subtract.</param>
        /// <param name="right">The second matrix to subtract.</param>
        /// <param name="result">When the method completes, contains the difference between the two matrices.</param>
        public static void Subtract(ref Matrix left, ref Matrix right, out Matrix result)
        {
            result.M11 = left.M11 - right.M11;
            result.M21 = left.M21 - right.M21;
            result.M31 = left.M31 - right.M31;
            result.M41 = left.M41 - right.M41;
            result.M12 = left.M12 - right.M12;
            result.M22 = left.M22 - right.M22;
            result.M32 = left.M32 - right.M32;
            result.M42 = left.M42 - right.M42;
            result.M13 = left.M13 - right.M13;
            result.M23 = left.M23 - right.M23;
            result.M33 = left.M33 - right.M33;
            result.M43 = left.M43 - right.M43;
            result.M14 = left.M14 - right.M14;
            result.M24 = left.M24 - right.M24;
            result.M34 = left.M34 - right.M34;
            result.M44 = left.M44 - right.M44;
        }

        /// <summary>
        /// Determines the difference between two matrices.
        /// </summary>
        /// <param name="left">The first matrix to subtract.</param>
        /// <param name="right">The second matrix to subtract.</param>
        /// <returns>The difference between the two matrices.</returns>
        public static Matrix Subtract(Matrix left, Matrix right)
        {
            Matrix result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Scales a matrix by the given value.
        /// </summary>
        /// <param name="left">The matrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <param name="result">When the method completes, contains the scaled matrix.</param>
        public static void Multiply(ref Matrix left, float right, out Matrix result)
        {
            result.M11 = left.M11 * right;
            result.M21 = left.M21 * right;
            result.M31 = left.M31 * right;
            result.M41 = left.M41 * right;
            result.M12 = left.M12 * right;
            result.M22 = left.M22 * right;
            result.M32 = left.M32 * right;
            result.M42 = left.M42 * right;
            result.M13 = left.M13 * right;
            result.M23 = left.M23 * right;
            result.M33 = left.M33 * right;
            result.M43 = left.M43 * right;
            result.M14 = left.M14 * right;
            result.M24 = left.M24 * right;
            result.M34 = left.M34 * right;
            result.M44 = left.M44 * right;
        }

        /// <summary>
        /// Scales a matrix by the given value.
        /// </summary>
        /// <param name="left">The matrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix Multiply(Matrix left, float right)
        {
            Matrix result;
            Multiply(ref left, right, out result);
            return result;
        }

        /// <summary>
        /// Determines the product of two matrices.
        /// Variables passed as <paramref name="left"/> or <paramref name="right"/> must not be used as the out parameter
        /// <paramref name="result"/>, because <paramref name="result"/> is calculated in-place.
        /// </summary>
        /// <param name="left">The first matrix to multiply.</param>
        /// <param name="right">The second matrix to multiply.</param>
        /// <param name="result">The product of the two matrices.</param>
        public static void MultiplyTo(ref Matrix left, ref Matrix right, out Matrix result)
        {
            result.M11 = (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31) + (left.M14 * right.M41);
            result.M21 = (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31) + (left.M24 * right.M41);
            result.M31 = (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31) + (left.M34 * right.M41);
            result.M41 = (left.M41 * right.M11) + (left.M42 * right.M21) + (left.M43 * right.M31) + (left.M44 * right.M41);
            result.M12 = (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32) + (left.M14 * right.M42);
            result.M22 = (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32) + (left.M24 * right.M42);
            result.M32 = (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32) + (left.M34 * right.M42);
            result.M42 = (left.M41 * right.M12) + (left.M42 * right.M22) + (left.M43 * right.M32) + (left.M44 * right.M42);
            result.M13 = (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33) + (left.M14 * right.M43);
            result.M23 = (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33) + (left.M24 * right.M43);
            result.M33 = (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33) + (left.M34 * right.M43);
            result.M43 = (left.M41 * right.M13) + (left.M42 * right.M23) + (left.M43 * right.M33) + (left.M44 * right.M43);
            result.M14 = (left.M11 * right.M14) + (left.M12 * right.M24) + (left.M13 * right.M34) + (left.M14 * right.M44);
            result.M24 = (left.M21 * right.M14) + (left.M22 * right.M24) + (left.M23 * right.M34) + (left.M24 * right.M44);
            result.M34 = (left.M31 * right.M14) + (left.M32 * right.M24) + (left.M33 * right.M34) + (left.M34 * right.M44);
            result.M44 = (left.M41 * right.M14) + (left.M42 * right.M24) + (left.M43 * right.M34) + (left.M44 * right.M44);
        }

        /// <summary>
        /// Determines the product of two matrices.
        /// Variables passed as <paramref name="left"/> or <paramref name="right"/> must not be used as the out parameter
        /// <paramref name="result"/>, because <paramref name="result"/> is calculated in-place.
        /// </summary>
        /// <param name="left">The first matrix to multiply.</param>
        /// <param name="right">The second matrix to multiply.</param>
        /// <param name="result">The product of the two matrices.</param>
        public static void Multiply(ref Matrix left, ref Matrix right, out Matrix result)
        {
            result.M11 = (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31) + (left.M14 * right.M41);
            result.M21 = (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31) + (left.M24 * right.M41);
            result.M31 = (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31) + (left.M34 * right.M41);
            result.M41 = (left.M41 * right.M11) + (left.M42 * right.M21) + (left.M43 * right.M31) + (left.M44 * right.M41);
            result.M12 = (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32) + (left.M14 * right.M42);
            result.M22 = (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32) + (left.M24 * right.M42);
            result.M32 = (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32) + (left.M34 * right.M42);
            result.M42 = (left.M41 * right.M12) + (left.M42 * right.M22) + (left.M43 * right.M32) + (left.M44 * right.M42);
            result.M13 = (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33) + (left.M14 * right.M43);
            result.M23 = (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33) + (left.M24 * right.M43);
            result.M33 = (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33) + (left.M34 * right.M43);
            result.M43 = (left.M41 * right.M13) + (left.M42 * right.M23) + (left.M43 * right.M33) + (left.M44 * right.M43);
            result.M14 = (left.M11 * right.M14) + (left.M12 * right.M24) + (left.M13 * right.M34) + (left.M14 * right.M44);
            result.M24 = (left.M21 * right.M14) + (left.M22 * right.M24) + (left.M23 * right.M34) + (left.M24 * right.M44);
            result.M34 = (left.M31 * right.M14) + (left.M32 * right.M24) + (left.M33 * right.M34) + (left.M34 * right.M44);
            result.M44 = (left.M41 * right.M14) + (left.M42 * right.M24) + (left.M43 * right.M34) + (left.M44 * right.M44);
        }

        /// <summary>
        /// Determines the product of two matrices.
        /// Variables passed as <paramref name="left"/> or <paramref name="right"/> must not be used as the out parameter
        /// <paramref name="result"/>, because <paramref name="result"/> is calculated in-place.
        /// </summary>
        /// <param name="left">The first matrix to multiply.</param>
        /// <param name="right">The second matrix to multiply.</param>
        /// <param name="result">The product of the two matrices.</param>
        public static void MultiplyRef(ref Matrix left, ref Matrix right, ref Matrix result)
        {
            result.M11 = (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31) + (left.M14 * right.M41);
            result.M21 = (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31) + (left.M24 * right.M41);
            result.M31 = (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31) + (left.M34 * right.M41);
            result.M41 = (left.M41 * right.M11) + (left.M42 * right.M21) + (left.M43 * right.M31) + (left.M44 * right.M41);
            result.M12 = (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32) + (left.M14 * right.M42);
            result.M22 = (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32) + (left.M24 * right.M42);
            result.M32 = (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32) + (left.M34 * right.M42);
            result.M42 = (left.M41 * right.M12) + (left.M42 * right.M22) + (left.M43 * right.M32) + (left.M44 * right.M42);
            result.M13 = (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33) + (left.M14 * right.M43);
            result.M23 = (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33) + (left.M24 * right.M43);
            result.M33 = (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33) + (left.M34 * right.M43);
            result.M43 = (left.M41 * right.M13) + (left.M42 * right.M23) + (left.M43 * right.M33) + (left.M44 * right.M43);
            result.M14 = (left.M11 * right.M14) + (left.M12 * right.M24) + (left.M13 * right.M34) + (left.M14 * right.M44);
            result.M24 = (left.M21 * right.M14) + (left.M22 * right.M24) + (left.M23 * right.M34) + (left.M24 * right.M44);
            result.M34 = (left.M31 * right.M14) + (left.M32 * right.M24) + (left.M33 * right.M34) + (left.M34 * right.M44);
            result.M44 = (left.M41 * right.M14) + (left.M42 * right.M24) + (left.M43 * right.M34) + (left.M44 * right.M44);
        }

        /// <summary>
        /// Determines the product of two matrices.
        /// </summary>
        /// <param name="left">The first matrix to multiply.</param>
        /// <param name="right">The second matrix to multiply.</param>
        /// <returns>The product of the two matrices.</returns>
        public static Matrix Multiply(Matrix left, Matrix right)
        {
            Matrix result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Scales a matrix by the given value.
        /// </summary>
        /// <param name="left">The matrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <param name="result">When the method completes, contains the scaled matrix.</param>
        public static void Divide(ref Matrix left, float right, out Matrix result)
        {
            float inv = 1.0f / right;

            result.M11 = left.M11 * inv;
            result.M21 = left.M21 * inv;
            result.M31 = left.M31 * inv;
            result.M41 = left.M41 * inv;
            result.M12 = left.M12 * inv;
            result.M22 = left.M22 * inv;
            result.M32 = left.M32 * inv;
            result.M42 = left.M42 * inv;
            result.M13 = left.M13 * inv;
            result.M23 = left.M23 * inv;
            result.M33 = left.M33 * inv;
            result.M43 = left.M43 * inv;
            result.M14 = left.M14 * inv;
            result.M24 = left.M24 * inv;
            result.M34 = left.M34 * inv;
            result.M44 = left.M44 * inv;
        }

        /// <summary>
        /// Scales a matrix by the given value.
        /// </summary>
        /// <param name="left">The matrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix Divide(Matrix left, float right)
        {
            Matrix result;
            Divide(ref left, right, out result);
            return result;
        }

        /// <summary>
        /// Determines the quotient of two matrices.
        /// </summary>
        /// <param name="left">The first matrix to divide.</param>
        /// <param name="right">The second matrix to divide.</param>
        /// <param name="result">When the method completes, contains the quotient of the two matrices.</param>
        public static void Divide(ref Matrix left, ref Matrix right, out Matrix result)
        {
            result.M11 = left.M11 / right.M11;
            result.M21 = left.M21 / right.M21;
            result.M31 = left.M31 / right.M31;
            result.M41 = left.M41 / right.M41;
            result.M12 = left.M12 / right.M12;
            result.M22 = left.M22 / right.M22;
            result.M32 = left.M32 / right.M32;
            result.M42 = left.M42 / right.M42;
            result.M13 = left.M13 / right.M13;
            result.M23 = left.M23 / right.M23;
            result.M33 = left.M33 / right.M33;
            result.M43 = left.M43 / right.M43;
            result.M14 = left.M14 / right.M14;
            result.M24 = left.M24 / right.M24;
            result.M34 = left.M34 / right.M34;
            result.M44 = left.M44 / right.M44;
        }

        /// <summary>
        /// Determines the quotient of two matrices.
        /// </summary>
        /// <param name="left">The first matrix to divide.</param>
        /// <param name="right">The second matrix to divide.</param>
        /// <returns>The quotient of the two matrices.</returns>
        public static Matrix Divide(Matrix left, Matrix right)
        {
            Matrix result;
            Divide(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Performs the exponential operation on a matrix.
        /// </summary>
        /// <param name="value">The matrix to perform the operation on.</param>
        /// <param name="exponent">The exponent to raise the matrix to.</param>
        /// <param name="result">When the method completes, contains the exponential matrix.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="exponent"/> is negative.</exception>
        public static void Exponent(ref Matrix value, int exponent, out Matrix result)
        {
            //Source: http://rosettacode.org
            //Refrence: http://rosettacode.org/wiki/Matrix-exponentiation_operator

            if (exponent < 0)
                throw new ArgumentOutOfRangeException("exponent", "The exponent can not be negative.");

            if (exponent == 0)
            {
                result = Matrix.Identity;
                return;
            }

            if (exponent == 1)
            {
                result = value;
                return;
            }

            Matrix identity = Matrix.Identity;
            Matrix temp = value;

            while (true)
            {
                if ((exponent & 1) != 0)
                    identity = identity * temp;

                exponent /= 2;

                if (exponent > 0)
                    temp *= temp;
                else
                    break;
            }

            result = identity;
        }

        /// <summary>
        /// Performs the exponential operation on a matrix.
        /// </summary>
        /// <param name="value">The matrix to perform the operation on.</param>
        /// <param name="exponent">The exponent to raise the matrix to.</param>
        /// <returns>The exponential matrix.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="exponent"/> is negative.</exception>
        public static Matrix Exponent(Matrix value, int exponent)
        {
            Matrix result;
            Exponent(ref value, exponent, out result);
            return result;
        }

        /// <summary>
        /// Negates a matrix.
        /// </summary>
        /// <param name="value">The matrix to be negated.</param>
        /// <param name="result">When the method completes, contains the negated matrix.</param>
        public static void Negate(ref Matrix value, out Matrix result)
        {
            result.M11 = -value.M11;
            result.M21 = -value.M21;
            result.M31 = -value.M31;
            result.M41 = -value.M41;
            result.M12 = -value.M12;
            result.M22 = -value.M22;
            result.M32 = -value.M32;
            result.M42 = -value.M42;
            result.M13 = -value.M13;
            result.M23 = -value.M23;
            result.M33 = -value.M33;
            result.M43 = -value.M43;
            result.M14 = -value.M14;
            result.M24 = -value.M24;
            result.M34 = -value.M34;
            result.M44 = -value.M44;
        }

        /// <summary>
        /// Negates a matrix.
        /// </summary>
        /// <param name="value">The matrix to be negated.</param>
        /// <returns>The negated matrix.</returns>
        public static Matrix Negate(Matrix value)
        {
            Matrix result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Performs a linear interpolation between two matrices.
        /// </summary>
        /// <param name="start">Start matrix.</param>
        /// <param name="end">End matrix.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the linear interpolation of the two matrices.</param>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
        /// </remarks>
        public static void Lerp(ref Matrix start, ref Matrix end, float amount, out Matrix result)
        {
            result.M11 = start.M11 + ((end.M11 - start.M11) * amount);
            result.M21 = start.M21 + ((end.M21 - start.M21) * amount);
            result.M31 = start.M31 + ((end.M31 - start.M31) * amount);
            result.M41 = start.M41 + ((end.M41 - start.M41) * amount);
            result.M12 = start.M12 + ((end.M12 - start.M12) * amount);
            result.M22 = start.M22 + ((end.M22 - start.M22) * amount);
            result.M32 = start.M32 + ((end.M32 - start.M32) * amount);
            result.M42 = start.M42 + ((end.M42 - start.M42) * amount);
            result.M13 = start.M13 + ((end.M13 - start.M13) * amount);
            result.M23 = start.M23 + ((end.M23 - start.M23) * amount);
            result.M33 = start.M33 + ((end.M33 - start.M33) * amount);
            result.M43 = start.M43 + ((end.M43 - start.M43) * amount);
            result.M14 = start.M14 + ((end.M14 - start.M14) * amount);
            result.M24 = start.M24 + ((end.M24 - start.M24) * amount);
            result.M34 = start.M34 + ((end.M34 - start.M34) * amount);
            result.M44 = start.M44 + ((end.M44 - start.M44) * amount);
        }

        /// <summary>
        /// Performs a linear interpolation between two matrices.
        /// </summary>
        /// <param name="start">Start matrix.</param>
        /// <param name="end">End matrix.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The linear interpolation of the two matrices.</returns>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
        /// </remarks>
        public static Matrix Lerp(Matrix start, Matrix end, float amount)
        {
            Matrix result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Performs a cubic interpolation between two matrices.
        /// </summary>
        /// <param name="start">Start matrix.</param>
        /// <param name="end">End matrix.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the cubic interpolation of the two matrices.</param>
        public static void SmoothStep(ref Matrix start, ref Matrix end, float amount, out Matrix result)
        {
            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = (amount * amount) * (3.0f - (2.0f * amount));

            result.M11 = start.M11 + ((end.M11 - start.M11) * amount);
            result.M21 = start.M21 + ((end.M21 - start.M21) * amount);
            result.M31 = start.M31 + ((end.M31 - start.M31) * amount);
            result.M41 = start.M41 + ((end.M41 - start.M41) * amount);
            result.M12 = start.M12 + ((end.M12 - start.M12) * amount);
            result.M22 = start.M22 + ((end.M22 - start.M22) * amount);
            result.M32 = start.M32 + ((end.M32 - start.M32) * amount);
            result.M42 = start.M42 + ((end.M42 - start.M42) * amount);
            result.M13 = start.M13 + ((end.M13 - start.M13) * amount);
            result.M23 = start.M23 + ((end.M23 - start.M23) * amount);
            result.M33 = start.M33 + ((end.M33 - start.M33) * amount);
            result.M43 = start.M43 + ((end.M43 - start.M43) * amount);
            result.M14 = start.M14 + ((end.M14 - start.M14) * amount);
            result.M24 = start.M24 + ((end.M24 - start.M24) * amount);
            result.M34 = start.M34 + ((end.M34 - start.M34) * amount);
            result.M44 = start.M44 + ((end.M44 - start.M44) * amount);
        }

        /// <summary>
        /// Performs a cubic interpolation between two matrices.
        /// </summary>
        /// <param name="start">Start matrix.</param>
        /// <param name="end">End matrix.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The cubic interpolation of the two matrices.</returns>
        public static Matrix SmoothStep(Matrix start, Matrix end, float amount)
        {
            Matrix result;
            SmoothStep(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Calculates the transpose of the specified matrix.
        /// </summary>
        /// <param name="value">The matrix whose transpose is to be calculated.</param>
        /// <param name="result">When the method completes, contains the transpose of the specified matrix.</param>
        public static void Transpose(ref Matrix value, out Matrix result)
        {
            result = new Matrix(
                value.M11,
                value.M21,
                value.M31,
                value.M41,
                value.M12,
                value.M22,
                value.M32,
                value.M42,
                value.M13,
                value.M23,
                value.M33,
                value.M43,
                value.M14,
                value.M24,
                value.M34,
                value.M44);
        }

        /// <summary>
        /// Calculates the transpose of the specified matrix.
        /// </summary>
        /// <param name="value">The matrix whose transpose is to be calculated.</param>
        /// <returns>The transpose of the specified matrix.</returns>
        public static Matrix Transpose(Matrix value)
        {
            value.Transpose();
            return value;
        }

        /// <summary>
        /// Calculates the inverse of the specified matrix.
        /// </summary>
        /// <param name="value">The matrix whose inverse is to be calculated.</param>
        /// <param name="result">When the method completes, contains the inverse of the specified matrix.</param>
        public static void Invert(ref Matrix value, out Matrix result)
        {
            float b0 = (value.M31 * value.M42) - (value.M32 * value.M41);
            float b1 = (value.M31 * value.M43) - (value.M33 * value.M41);
            float b2 = (value.M34 * value.M41) - (value.M31 * value.M44);
            float b3 = (value.M32 * value.M43) - (value.M33 * value.M42);
            float b4 = (value.M34 * value.M42) - (value.M32 * value.M44);
            float b5 = (value.M33 * value.M44) - (value.M34 * value.M43);

            float d11 = value.M22 * b5 + value.M23 * b4 + value.M24 * b3;
            float d12 = value.M21 * b5 + value.M23 * b2 + value.M24 * b1;
            float d13 = value.M21 * -b4 + value.M22 * b2 + value.M24 * b0;
            float d14 = value.M21 * b3 + value.M22 * -b1 + value.M23 * b0;

            float det = value.M11 * d11 - value.M12 * d12 + value.M13 * d13 - value.M14 * d14;
            if (Math.Abs(det) == 0.0f)
            {
                result = Matrix.Zero;
                return;
            }

            det = 1f / det;

            float a0 = (value.M11 * value.M22) - (value.M12 * value.M21);
            float a1 = (value.M11 * value.M23) - (value.M13 * value.M21);
            float a2 = (value.M14 * value.M21) - (value.M11 * value.M24);
            float a3 = (value.M12 * value.M23) - (value.M13 * value.M22);
            float a4 = (value.M14 * value.M22) - (value.M12 * value.M24);
            float a5 = (value.M13 * value.M24) - (value.M14 * value.M23);

            float d21 = value.M12 * b5 + value.M13 * b4 + value.M14 * b3;
            float d22 = value.M11 * b5 + value.M13 * b2 + value.M14 * b1;
            float d23 = value.M11 * -b4 + value.M12 * b2 + value.M14 * b0;
            float d24 = value.M11 * b3 + value.M12 * -b1 + value.M13 * b0;

            float d31 = value.M42 * a5 + value.M43 * a4 + value.M44 * a3;
            float d32 = value.M41 * a5 + value.M43 * a2 + value.M44 * a1;
            float d33 = value.M41 * -a4 + value.M42 * a2 + value.M44 * a0;
            float d34 = value.M41 * a3 + value.M42 * -a1 + value.M43 * a0;

            float d41 = value.M32 * a5 + value.M33 * a4 + value.M34 * a3;
            float d42 = value.M31 * a5 + value.M33 * a2 + value.M34 * a1;
            float d43 = value.M31 * -a4 + value.M32 * a2 + value.M34 * a0;
            float d44 = value.M31 * a3 + value.M32 * -a1 + value.M33 * a0;

            result.M11 = +d11 * det; result.M12 = -d21 * det; result.M13 = +d31 * det; result.M14 = -d41 * det;
            result.M21 = -d12 * det; result.M22 = +d22 * det; result.M23 = -d32 * det; result.M24 = +d42 * det;
            result.M31 = +d13 * det; result.M32 = -d23 * det; result.M33 = +d33 * det; result.M34 = -d43 * det;
            result.M41 = -d14 * det; result.M42 = +d24 * det; result.M43 = -d34 * det; result.M44 = +d44 * det;
        }

        /// <summary>
        /// Calculates the inverse of the specified matrix.
        /// </summary>
        /// <param name="value">The matrix whose inverse is to be calculated.</param>
        /// <returns>The inverse of the specified matrix.</returns>
        public static Matrix Invert(Matrix value)
        {
            value.Invert();
            return value;
        }

        /// <summary>
        /// Orthogonalizes the specified matrix.
        /// </summary>
        /// <param name="value">The matrix to orthogonalize.</param>
        /// <param name="result">When the method completes, contains the orthogonalized matrix.</param>
        /// <remarks>
        /// <para>Orthogonalization is the process of making all rows orthogonal to each other. This
        /// means that any given row in the matrix will be orthogonal to any other given row in the
        /// matrix.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting matrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the matrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static void Orthogonalize(ref Matrix value, out Matrix result)
        {
            //Uses the modified Gram-Schmidt process.
            //q1 = m1
            //q2 = m2 - ((q1 ⋅ m2) / (q1 ⋅ q1)) * q1
            //q3 = m3 - ((q1 ⋅ m3) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m3) / (q2 ⋅ q2)) * q2
            //q4 = m4 - ((q1 ⋅ m4) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m4) / (q2 ⋅ q2)) * q2 - ((q3 ⋅ m4) / (q3 ⋅ q3)) * q3

            //By separating the above algorithm into multiple lines, we actually increase accuracy.
            result = value;

            result.Row2 = result.Row2 - (Vector4.Dot(result.Row1, result.Row2) / Vector4.Dot(result.Row1, result.Row1)) * result.Row1;

            result.Row3 = result.Row3 - (Vector4.Dot(result.Row1, result.Row3) / Vector4.Dot(result.Row1, result.Row1)) * result.Row1;
            result.Row3 = result.Row3 - (Vector4.Dot(result.Row2, result.Row3) / Vector4.Dot(result.Row2, result.Row2)) * result.Row2;

            result.Row4 = result.Row4 - (Vector4.Dot(result.Row1, result.Row4) / Vector4.Dot(result.Row1, result.Row1)) * result.Row1;
            result.Row4 = result.Row4 - (Vector4.Dot(result.Row2, result.Row4) / Vector4.Dot(result.Row2, result.Row2)) * result.Row2;
            result.Row4 = result.Row4 - (Vector4.Dot(result.Row3, result.Row4) / Vector4.Dot(result.Row3, result.Row3)) * result.Row3;
        }

        /// <summary>
        /// Orthogonalizes the specified matrix.
        /// </summary>
        /// <param name="value">The matrix to orthogonalize.</param>
        /// <returns>The orthogonalized matrix.</returns>
        /// <remarks>
        /// <para>Orthogonalization is the process of making all rows orthogonal to each other. This
        /// means that any given row in the matrix will be orthogonal to any other given row in the
        /// matrix.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting matrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the matrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static Matrix Orthogonalize(Matrix value)
        {
            Matrix result;
            Orthogonalize(ref value, out result);
            return result;
        }

        /// <summary>
        /// Orthonormalizes the specified matrix.
        /// </summary>
        /// <param name="value">The matrix to orthonormalize.</param>
        /// <param name="result">When the method completes, contains the orthonormalized matrix.</param>
        /// <remarks>
        /// <para>Orthonormalization is the process of making all rows and columns orthogonal to each
        /// other and making all rows and columns of unit length. This means that any given row will
        /// be orthogonal to any other given row and any given column will be orthogonal to any other
        /// given column. Any given row will not be orthogonal to any given column. Every row and every
        /// column will be of unit length.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting matrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the matrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static void Orthonormalize(ref Matrix value, out Matrix result)
        {
            //Uses the modified Gram-Schmidt process.
            //Because we are making unit vectors, we can optimize the math for orthogonalization
            //and simplify the projection operation to remove the division.
            //q1 = m1 / |m1|
            //q2 = (m2 - (q1 ⋅ m2) * q1) / |m2 - (q1 ⋅ m2) * q1|
            //q3 = (m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2) / |m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2|
            //q4 = (m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3) / |m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3|

            //By separating the above algorithm into multiple lines, we actually increase accuracy.
            result = value;

            result.Row1 = Vector4.Normalize(result.Row1);

            result.Row2 = result.Row2 - Vector4.Dot(result.Row1, result.Row2) * result.Row1;
            result.Row2 = Vector4.Normalize(result.Row2);

            result.Row3 = result.Row3 - Vector4.Dot(result.Row1, result.Row3) * result.Row1;
            result.Row3 = result.Row3 - Vector4.Dot(result.Row2, result.Row3) * result.Row2;
            result.Row3 = Vector4.Normalize(result.Row3);

            result.Row4 = result.Row4 - Vector4.Dot(result.Row1, result.Row4) * result.Row1;
            result.Row4 = result.Row4 - Vector4.Dot(result.Row2, result.Row4) * result.Row2;
            result.Row4 = result.Row4 - Vector4.Dot(result.Row3, result.Row4) * result.Row3;
            result.Row4 = Vector4.Normalize(result.Row4);
        }

        /// <summary>
        /// Orthonormalizes the specified matrix.
        /// </summary>
        /// <param name="value">The matrix to orthonormalize.</param>
        /// <returns>The orthonormalized matrix.</returns>
        /// <remarks>
        /// <para>Orthonormalization is the process of making all rows and columns orthogonal to each
        /// other and making all rows and columns of unit length. This means that any given row will
        /// be orthogonal to any other given row and any given column will be orthogonal to any other
        /// given column. Any given row will not be orthogonal to any given column. Every row and every
        /// column will be of unit length.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting matrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the matrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static Matrix Orthonormalize(Matrix value)
        {
            Matrix result;
            Orthonormalize(ref value, out result);
            return result;
        }

        /// <summary>
        /// Brings the matrix into upper triangular form using elementry row operations.
        /// </summary>
        /// <param name="value">The matrix to put into upper triangular form.</param>
        /// <param name="result">When the method completes, contains the upper triangular matrix.</param>
        /// <remarks>
        /// If the matrix is not invertable (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the matrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static void UpperTriangularForm(ref Matrix value, out Matrix result)
        {
            //Adapted from the row echelon code.
            result = value;
            int lead = 0;
            int rowcount = 4;
            int columncount = 4;

            for (int r = 0; r < rowcount; ++r)
            {
                if (columncount <= lead)
                    return;

                int i = r;

                while (Math.Abs(result[i, lead]) < MathUtil.ZeroTolerance)
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (lead == columncount)
                            return;
                    }
                }

                if (i != r)
                {
                    result.ExchangeRows(i, r);
                }

                float multiplier = 1f / result[r, lead];

                for (; i < rowcount; ++i)
                {
                    if (i != r)
                    {
                        result[i, 0] -= result[r, 0] * multiplier * result[i, lead];
                        result[i, 1] -= result[r, 1] * multiplier * result[i, lead];
                        result[i, 2] -= result[r, 2] * multiplier * result[i, lead];
                        result[i, 3] -= result[r, 3] * multiplier * result[i, lead];
                    }
                }

                lead++;
            }
        }

        /// <summary>
        /// Brings the matrix into upper triangular form using elementry row operations.
        /// </summary>
        /// <param name="value">The matrix to put into upper triangular form.</param>
        /// <returns>The upper triangular matrix.</returns>
        /// <remarks>
        /// If the matrix is not invertable (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the matrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static Matrix UpperTriangularForm(Matrix value)
        {
            Matrix result;
            UpperTriangularForm(ref value, out result);
            return result;
        }

        /// <summary>
        /// Brings the matrix into lower triangular form using elementry row operations.
        /// </summary>
        /// <param name="value">The matrix to put into lower triangular form.</param>
        /// <param name="result">When the method completes, contains the lower triangular matrix.</param>
        /// <remarks>
        /// If the matrix is not invertable (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the matrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static void LowerTriangularForm(ref Matrix value, out Matrix result)
        {
            //Adapted from the row echelon code.
            Matrix temp = value;
            Matrix.Transpose(ref temp, out result);

            int lead = 0;
            int rowcount = 4;
            int columncount = 4;

            for (int r = 0; r < rowcount; ++r)
            {
                if (columncount <= lead)
                    return;

                int i = r;

                while (Math.Abs(result[i, lead]) < MathUtil.ZeroTolerance)
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (lead == columncount)
                            return;
                    }
                }

                if (i != r)
                {
                    result.ExchangeRows(i, r);
                }

                float multiplier = 1f / result[r, lead];

                for (; i < rowcount; ++i)
                {
                    if (i != r)
                    {
                        result[i, 0] -= result[r, 0] * multiplier * result[i, lead];
                        result[i, 1] -= result[r, 1] * multiplier * result[i, lead];
                        result[i, 2] -= result[r, 2] * multiplier * result[i, lead];
                        result[i, 3] -= result[r, 3] * multiplier * result[i, lead];
                    }
                }

                lead++;
            }

            Matrix.Transpose(ref result, out result);
        }

        /// <summary>
        /// Brings the matrix into lower triangular form using elementry row operations.
        /// </summary>
        /// <param name="value">The matrix to put into lower triangular form.</param>
        /// <returns>The lower triangular matrix.</returns>
        /// <remarks>
        /// If the matrix is not invertable (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the matrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static Matrix LowerTriangularForm(Matrix value)
        {
            Matrix result;
            LowerTriangularForm(ref value, out result);
            return result;
        }

        /// <summary>
        /// Brings the matrix into row echelon form using elementry row operations;
        /// </summary>
        /// <param name="value">The matrix to put into row echelon form.</param>
        /// <param name="result">When the method completes, contains the row echelon form of the matrix.</param>
        public static void RowEchelonForm(ref Matrix value, out Matrix result)
        {
            //Source: Wikipedia psuedo code
            //Reference: http://en.wikipedia.org/wiki/Row_echelon_form#Pseudocode

            result = value;
            int lead = 0;
            int rowcount = 4;
            int columncount = 4;

            for (int r = 0; r < rowcount; ++r)
            {
                if (columncount <= lead)
                    return;

                int i = r;

                while (Math.Abs(result[i, lead]) < MathUtil.ZeroTolerance)
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (lead == columncount)
                            return;
                    }
                }

                if (i != r)
                {
                    result.ExchangeRows(i, r);
                }

                float multiplier = 1f / result[r, lead];
                result[r, 0] *= multiplier;
                result[r, 1] *= multiplier;
                result[r, 2] *= multiplier;
                result[r, 3] *= multiplier;

                for (; i < rowcount; ++i)
                {
                    if (i != r)
                    {
                        result[i, 0] -= result[r, 0] * result[i, lead];
                        result[i, 1] -= result[r, 1] * result[i, lead];
                        result[i, 2] -= result[r, 2] * result[i, lead];
                        result[i, 3] -= result[r, 3] * result[i, lead];
                    }
                }

                lead++;
            }
        }

        /// <summary>
        /// Brings the matrix into row echelon form using elementry row operations;
        /// </summary>
        /// <param name="value">The matrix to put into row echelon form.</param>
        /// <returns>When the method completes, contains the row echelon form of the matrix.</returns>
        public static Matrix RowEchelonForm(Matrix value)
        {
            Matrix result;
            RowEchelonForm(ref value, out result);
            return result;
        }

        /// <summary>
        /// Brings the matrix into reduced row echelon form using elementry row operations.
        /// </summary>
        /// <param name="value">The matrix to put into reduced row echelon form.</param>
        /// <param name="augment">The fifth column of the matrix.</param>
        /// <param name="result">When the method completes, contains the resultant matrix after the operation.</param>
        /// <param name="augmentResult">When the method completes, contains the resultant fifth column of the matrix.</param>
        /// <remarks>
        /// <para>The fifth column is often called the agumented part of the matrix. This is because the fifth
        /// column is really just an extension of the matrix so that there is a place to put all of the
        /// non-zero components after the operation is complete.</para>
        /// <para>Often times the resultant matrix will the identity matrix or a matrix similar to the identity
        /// matrix. Sometimes, however, that is not possible and numbers other than zero and one may appear.</para>
        /// <para>This method can be used to solve systems of linear equations. Upon completion of this method,
        /// the <paramref name="augmentResult"/> will contain the solution for the system. It is up to the user
        /// to analyze both the input and the result to determine if a solution really exists.</para>
        /// </remarks>
        public static void ReducedRowEchelonForm(ref Matrix value, ref Vector4 augment, out Matrix result, out Vector4 augmentResult)
        {
            //Source: http://rosettacode.org
            //Reference: http://rosettacode.org/wiki/Reduced_row_echelon_form

            float[,] matrix = new float[4, 5];

            matrix[0, 0] = value[0, 0];
            matrix[0, 1] = value[0, 1];
            matrix[0, 2] = value[0, 2];
            matrix[0, 3] = value[0, 3];
            matrix[0, 4] = augment[0];

            matrix[1, 0] = value[1, 0];
            matrix[1, 1] = value[1, 1];
            matrix[1, 2] = value[1, 2];
            matrix[1, 3] = value[1, 3];
            matrix[1, 4] = augment[1];

            matrix[2, 0] = value[2, 0];
            matrix[2, 1] = value[2, 1];
            matrix[2, 2] = value[2, 2];
            matrix[2, 3] = value[2, 3];
            matrix[2, 4] = augment[2];

            matrix[3, 0] = value[3, 0];
            matrix[3, 1] = value[3, 1];
            matrix[3, 2] = value[3, 2];
            matrix[3, 3] = value[3, 3];
            matrix[3, 4] = augment[3];

            int lead = 0;
            int rowcount = 4;
            int columncount = 5;

            for (int r = 0; r < rowcount; r++)
            {
                if (columncount <= lead)
                    break;

                int i = r;

                while (matrix[i, lead] == 0)
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (columncount == lead)
                            break;
                    }
                }

                for (int j = 0; j < columncount; j++)
                {
                    float temp = matrix[r, j];
                    matrix[r, j] = matrix[i, j];
                    matrix[i, j] = temp;
                }

                float div = matrix[r, lead];

                for (int j = 0; j < columncount; j++)
                {
                    matrix[r, j] /= div;
                }

                for (int j = 0; j < rowcount; j++)
                {
                    if (j != r)
                    {
                        float sub = matrix[j, lead];
                        for (int k = 0; k < columncount; k++) matrix[j, k] -= (sub * matrix[r, k]);
                    }
                }

                lead++;
            }

            result.M11 = matrix[0, 0];
            result.M12 = matrix[0, 1];
            result.M13 = matrix[0, 2];
            result.M14 = matrix[0, 3];

            result.M21 = matrix[1, 0];
            result.M22 = matrix[1, 1];
            result.M23 = matrix[1, 2];
            result.M24 = matrix[1, 3];

            result.M31 = matrix[2, 0];
            result.M32 = matrix[2, 1];
            result.M33 = matrix[2, 2];
            result.M34 = matrix[2, 3];

            result.M41 = matrix[3, 0];
            result.M42 = matrix[3, 1];
            result.M43 = matrix[3, 2];
            result.M44 = matrix[3, 3];

            augmentResult.X = matrix[0, 4];
            augmentResult.Y = matrix[1, 4];
            augmentResult.Z = matrix[2, 4];
            augmentResult.W = matrix[3, 4];
        }

        /// <summary>
        /// Creates a spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <param name="result">When the method completes, contains the created billboard matrix.</param>
        public static void Billboard(ref Vector3 objectPosition, ref Vector3 cameraPosition, ref Vector3 cameraUpVector, ref Vector3 cameraForwardVector, out Matrix result)
        {
            Vector3 crossed;
            Vector3 final;
            Vector3 difference = objectPosition - cameraPosition;

            float lengthSq = difference.LengthSquared();
            if (lengthSq < MathUtil.ZeroTolerance)
                difference = -cameraForwardVector;
            else
                difference *= (float)(1.0 / Math.Sqrt(lengthSq));

            Vector3.Cross(ref cameraUpVector, ref difference, out crossed);
            crossed.Normalize();
            Vector3.Cross(ref difference, ref crossed, out final);

            result.M11 = crossed.X;
            result.M12 = crossed.Y;
            result.M13 = crossed.Z;
            result.M14 = 0.0f;
            result.M21 = final.X;
            result.M22 = final.Y;
            result.M23 = final.Z;
            result.M24 = 0.0f;
            result.M31 = difference.X;
            result.M32 = difference.Y;
            result.M33 = difference.Z;
            result.M34 = 0.0f;
            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1.0f;
        }

        /// <summary>
        /// Creates a spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">The position of the object around which the billboard will rotate.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <returns>The created billboard matrix.</returns>
        public static Matrix Billboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
        {
            Matrix result;
            Billboard(ref objectPosition, ref cameraPosition, ref cameraUpVector, ref cameraForwardVector, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, look-at matrix.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at matrix.</param>
        public static void LookAtLH(ref Vector3 eye, ref Vector3 target, ref Vector3 up, out Matrix result)
        {
            Vector3 xaxis, yaxis, zaxis;
            Vector3.Subtract(ref target, ref eye, out zaxis); zaxis.Normalize();
            Vector3.Cross(ref up, ref zaxis, out xaxis); xaxis.Normalize();
            Vector3.Cross(ref zaxis, ref xaxis, out yaxis);

            result = Matrix.Identity;
            result.M11 = xaxis.X; result.M21 = xaxis.Y; result.M31 = xaxis.Z;
            result.M12 = yaxis.X; result.M22 = yaxis.Y; result.M32 = yaxis.Z;
            result.M13 = zaxis.X; result.M23 = zaxis.Y; result.M33 = zaxis.Z;

            Vector3.Dot(ref xaxis, ref eye, out result.M41);
            Vector3.Dot(ref yaxis, ref eye, out result.M42);
            Vector3.Dot(ref zaxis, ref eye, out result.M43);

            result.M41 = -result.M41;
            result.M42 = -result.M42;
            result.M43 = -result.M43;
        }

        /// <summary>
        /// Creates a left-handed, look-at matrix.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <returns>The created look-at matrix.</returns>
        public static Matrix LookAtLH(Vector3 eye, Vector3 target, Vector3 up)
        {
            Matrix result;
            LookAtLH(ref eye, ref target, ref up, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, look-at matrix.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at matrix.</param>
        public static void LookAtRH(ref Vector3 eye, ref Vector3 target, ref Vector3 up, out Matrix result)
        {
            Vector3 xaxis, yaxis, zaxis;
            Vector3.Subtract(ref eye, ref target, out zaxis); zaxis.Normalize();
            Vector3.Cross(ref up, ref zaxis, out xaxis); xaxis.Normalize();
            Vector3.Cross(ref zaxis, ref xaxis, out yaxis);

            result = Matrix.Identity;
            result.M11 = xaxis.X; result.M21 = xaxis.Y; result.M31 = xaxis.Z;
            result.M12 = yaxis.X; result.M22 = yaxis.Y; result.M32 = yaxis.Z;
            result.M13 = zaxis.X; result.M23 = zaxis.Y; result.M33 = zaxis.Z;

            Vector3.Dot(ref xaxis, ref eye, out result.M41);
            Vector3.Dot(ref yaxis, ref eye, out result.M42);
            Vector3.Dot(ref zaxis, ref eye, out result.M43);

            result.M41 = -result.M41;
            result.M42 = -result.M42;
            result.M43 = -result.M43;
        }

        /// <summary>
        /// Creates a right-handed, look-at matrix.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <returns>The created look-at matrix.</returns>
        public static Matrix LookAtRH(Vector3 eye, Vector3 target, Vector3 up)
        {
            Matrix result;
            LookAtRH(ref eye, ref target, ref up, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, orthographic projection matrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void OrthoLH(float width, float height, float znear, float zfar, out Matrix result)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            OrthoOffCenterLH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a left-handed, orthographic projection matrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix OrthoLH(float width, float height, float znear, float zfar)
        {
            Matrix result;
            OrthoLH(width, height, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, orthographic projection matrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void OrthoRH(float width, float height, float znear, float zfar, out Matrix result)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            OrthoOffCenterRH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a right-handed, orthographic projection matrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix OrthoRH(float width, float height, float znear, float zfar)
        {
            Matrix result;
            OrthoRH(width, height, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, customized orthographic projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void OrthoOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar, out Matrix result)
        {
            float zRange = 1.0f / (zfar - znear);

            result = Matrix.Identity;
            result.M11 = 2.0f / (right - left);
            result.M22 = 2.0f / (top - bottom);
            result.M33 = zRange;
            result.M41 = (left + right) / (left - right);
            result.M42 = (top + bottom) / (bottom - top);
            result.M43 = -znear * zRange;
        }

        /// <summary>
        /// Creates a left-handed, customized orthographic projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix OrthoOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar)
        {
            Matrix result;
            OrthoOffCenterLH(left, right, bottom, top, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, customized orthographic projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void OrthoOffCenterRH(float left, float right, float bottom, float top, float znear, float zfar, out Matrix result)
        {
            OrthoOffCenterLH(left, right, bottom, top, znear, zfar, out result);
            result.M33 *= -1.0f;
        }

        /// <summary>
        /// Creates a right-handed, customized orthographic projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix OrthoOffCenterRH(float left, float right, float bottom, float top, float znear, float zfar)
        {
            Matrix result;
            OrthoOffCenterRH(left, right, bottom, top, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, perspective projection matrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void PerspectiveLH(float width, float height, float znear, float zfar, out Matrix result)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            PerspectiveOffCenterLH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a left-handed, perspective projection matrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix PerspectiveLH(float width, float height, float znear, float zfar)
        {
            Matrix result;
            PerspectiveLH(width, height, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, perspective projection matrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void PerspectiveRH(float width, float height, float znear, float zfar, out Matrix result)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            PerspectiveOffCenterRH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a right-handed, perspective projection matrix.
        /// </summary>
        /// <param name="width">Width of the viewing volume.</param>
        /// <param name="height">Height of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix PerspectiveRH(float width, float height, float znear, float zfar)
        {
            Matrix result;
            PerspectiveRH(width, height, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, perspective projection matrix based on a field of view.
        /// </summary>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="aspect">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void PerspectiveFovLH(float fov, float aspect, float znear, float zfar, out Matrix result)
        {
            float yScale = (float)(1.0 / Math.Tan(fov * 0.5f));
            float xScale = yScale / aspect;

            float halfWidth = znear / xScale;
            float halfHeight = znear / yScale;

            PerspectiveOffCenterLH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a left-handed, perspective projection matrix based on a field of view.
        /// </summary>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="aspect">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix PerspectiveFovLH(float fov, float aspect, float znear, float zfar)
        {
            Matrix result;
            PerspectiveFovLH(fov, aspect, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, perspective projection matrix based on a field of view.
        /// </summary>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="aspect">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void PerspectiveFovRH(float fov, float aspect, float znear, float zfar, out Matrix result)
        {
            float yScale = (float)(1.0 / Math.Tan(fov * 0.5f));
            float xScale = yScale / aspect;

            float halfWidth = znear / xScale;
            float halfHeight = znear / yScale;

            PerspectiveOffCenterRH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar, out result);
        }

        /// <summary>
        /// Creates a right-handed, perspective projection matrix based on a field of view.
        /// </summary>
        /// <param name="fov">Field of view in the y direction, in radians.</param>
        /// <param name="aspect">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix PerspectiveFovRH(float fov, float aspect, float znear, float zfar)
        {
            Matrix result;
            PerspectiveFovRH(fov, aspect, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a left-handed, customized perspective projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void PerspectiveOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar, out Matrix result)
        {
            float zRange = zfar / (zfar - znear);

            result = new Matrix();
            result.M11 = 2.0f * znear / (right - left);
            result.M22 = 2.0f * znear / (top - bottom);
            result.M31 = (left + right) / (left - right);
            result.M32 = (top + bottom) / (bottom - top);
            result.M33 = zRange;
            result.M34 = 1.0f;
            result.M43 = -znear * zRange;
        }

        /// <summary>
        /// Creates a left-handed, customized perspective projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix PerspectiveOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar)
        {
            Matrix result;
            PerspectiveOffCenterLH(left, right, bottom, top, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Creates a right-handed, customized perspective projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <param name="result">When the method completes, contains the created projection matrix.</param>
        public static void PerspectiveOffCenterRH(float left, float right, float bottom, float top, float znear, float zfar, out Matrix result)
        {
            PerspectiveOffCenterLH(left, right, bottom, top, znear, zfar, out result);
            result.M31 *= -1.0f;
            result.M32 *= -1.0f;
            result.M33 *= -1.0f;
            result.M34 *= -1.0f;
        }

        /// <summary>
        /// Creates a right-handed, customized perspective projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the viewing volume.</param>
        /// <param name="right">Maximum x-value of the viewing volume.</param>
        /// <param name="bottom">Minimum y-value of the viewing volume.</param>
        /// <param name="top">Maximum y-value of the viewing volume.</param>
        /// <param name="znear">Minimum z-value of the viewing volume.</param>
        /// <param name="zfar">Maximum z-value of the viewing volume.</param>
        /// <returns>The created projection matrix.</returns>
        public static Matrix PerspectiveOffCenterRH(float left, float right, float bottom, float top, float znear, float zfar)
        {
            Matrix result;
            PerspectiveOffCenterRH(left, right, bottom, top, znear, zfar, out result);
            return result;
        }

        /// <summary>
        /// Builds a matrix that can be used to reflect vectors about a plane.
        /// </summary>
        /// <param name="plane">The plane for which the reflection occurs. This parameter is assumed to be normalized.</param>
        /// <param name="result">When the method completes, contains the reflection matrix.</param>
        public static void Reflection(ref Plane plane, out Matrix result)
        {
            float x = plane.Normal.X;
            float y = plane.Normal.Y;
            float z = plane.Normal.Z;
            float x2 = -2.0f * x;
            float y2 = -2.0f * y;
            float z2 = -2.0f * z;

            result.M11 = (x2 * x) + 1.0f;
            result.M12 = y2 * x;
            result.M13 = z2 * x;
            result.M14 = 0.0f;
            result.M21 = x2 * y;
            result.M22 = (y2 * y) + 1.0f;
            result.M23 = z2 * y;
            result.M24 = 0.0f;
            result.M31 = x2 * z;
            result.M32 = y2 * z;
            result.M33 = (z2 * z) + 1.0f;
            result.M34 = 0.0f;
            result.M41 = x2 * plane.D;
            result.M42 = y2 * plane.D;
            result.M43 = z2 * plane.D;
            result.M44 = 1.0f;
        }

        /// <summary>
        /// Builds a matrix that can be used to reflect vectors about a plane.
        /// </summary>
        /// <param name="plane">The plane for which the reflection occurs. This parameter is assumed to be normalized.</param>
        /// <returns>The reflection matrix.</returns>
        public static Matrix Reflection(Plane plane)
        {
            Matrix result;
            Reflection(ref plane, out result);
            return result;
        }

        /// <summary>
        /// Creates a matrix that flattens geometry into a shadow.
        /// </summary>
        /// <param name="light">The light direction. If the W component is 0, the light is directional light; if the
        /// W component is 1, the light is a point light.</param>
        /// <param name="plane">The plane onto which to project the geometry as a shadow. This parameter is assumed to be normalized.</param>
        /// <param name="result">When the method completes, contains the shadow matrix.</param>
        public static void Shadow(ref Vector4 light, ref Plane plane, out Matrix result)
        {
            float dot = (plane.Normal.X * light.X) + (plane.Normal.Y * light.Y) + (plane.Normal.Z * light.Z) + (plane.D * light.W);
            float x = -plane.Normal.X;
            float y = -plane.Normal.Y;
            float z = -plane.Normal.Z;
            float d = -plane.D;

            result.M11 = (x * light.X) + dot;
            result.M21 = y * light.X;
            result.M31 = z * light.X;
            result.M41 = d * light.X;
            result.M12 = x * light.Y;
            result.M22 = (y * light.Y) + dot;
            result.M32 = z * light.Y;
            result.M42 = d * light.Y;
            result.M13 = x * light.Z;
            result.M23 = y * light.Z;
            result.M33 = (z * light.Z) + dot;
            result.M43 = d * light.Z;
            result.M14 = x * light.W;
            result.M24 = y * light.W;
            result.M34 = z * light.W;
            result.M44 = (d * light.W) + dot;
        }

        /// <summary>
        /// Creates a matrix that flattens geometry into a shadow.
        /// </summary>
        /// <param name="light">The light direction. If the W component is 0, the light is directional light; if the
        /// W component is 1, the light is a point light.</param>
        /// <param name="plane">The plane onto which to project the geometry as a shadow. This parameter is assumed to be normalized.</param>
        /// <returns>The shadow matrix.</returns>
        public static Matrix Shadow(Vector4 light, Plane plane)
        {
            Matrix result;
            Shadow(ref light, ref plane, out result);
            return result;
        }

        /// <summary>
        /// Creates a matrix that scales along the x-axis, y-axis, and y-axis.
        /// </summary>
        /// <param name="scale">Scaling factor for all three axes.</param>
        /// <param name="result">When the method completes, contains the created scaling matrix.</param>
        public static void Scaling(ref Vector3 scale, out Matrix result)
        {
            Scaling(scale.X, scale.Y, scale.Z, out result);
        }

        /// <summary>
        /// Creates a matrix that scales along the x-axis, y-axis, and y-axis.
        /// </summary>
        /// <param name="scale">Scaling factor for all three axes.</param>
        /// <returns>The created scaling matrix.</returns>
        public static Matrix Scaling(Vector3 scale)
        {
            Matrix result;
            Scaling(ref scale, out result);
            return result;
        }

        /// <summary>
        /// Creates a matrix that scales along the x-axis, y-axis, and y-axis.
        /// </summary>
        /// <param name="x">Scaling factor that is applied along the x-axis.</param>
        /// <param name="y">Scaling factor that is applied along the y-axis.</param>
        /// <param name="z">Scaling factor that is applied along the z-axis.</param>
        /// <param name="result">When the method completes, contains the created scaling matrix.</param>
        public static void Scaling(float x, float y, float z, out Matrix result)
        {
            result = Matrix.Identity;
            result.M11 = x;
            result.M22 = y;
            result.M33 = z;
        }

        /// <summary>
        /// Creates a matrix that scales along the x-axis, y-axis, and y-axis.
        /// </summary>
        /// <param name="x">Scaling factor that is applied along the x-axis.</param>
        /// <param name="y">Scaling factor that is applied along the y-axis.</param>
        /// <param name="z">Scaling factor that is applied along the z-axis.</param>
        /// <returns>The created scaling matrix.</returns>
        public static Matrix Scaling(float x, float y, float z)
        {
            Matrix result;
            Scaling(x, y, z, out result);
            return result;
        }

        /// <summary>
        /// Creates a matrix that uniformally scales along all three axis.
        /// </summary>
        /// <param name="scale">The uniform scale that is applied along all axis.</param>
        /// <param name="result">When the method completes, contains the created scaling matrix.</param>
        public static void Scaling(float scale, out Matrix result)
        {
            result = Matrix.Identity;
            result.M11 = result.M22 = result.M33 = scale;
        }

        /// <summary>
        /// Creates a matrix that uniformally scales along all three axis.
        /// </summary>
        /// <param name="scale">The uniform scale that is applied along all axis.</param>
        /// <returns>The created scaling matrix.</returns>
        public static Matrix Scaling(float scale)
        {
            Matrix result;
            Scaling(scale, out result);
            return result;
        }

        /// <summary>
        /// Creates a matrix that rotates around the x-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="result">When the method completes, contains the created rotation matrix.</param>
        public static void RotationX(float angle, out Matrix result)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            result = Matrix.Identity;
            result.M22 = cos;
            result.M23 = sin;
            result.M32 = -sin;
            result.M33 = cos;
        }

        /// <summary>
        /// Creates a matrix that rotates around the x-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <returns>The created rotation matrix.</returns>
        public static Matrix RotationX(float angle)
        {
            Matrix result;
            RotationX(angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a matrix that rotates around the y-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="result">When the method completes, contains the created rotation matrix.</param>
        public static void RotationY(float angle, out Matrix result)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            result = Matrix.Identity;
            result.M11 = cos;
            result.M13 = -sin;
            result.M31 = sin;
            result.M33 = cos;
        }

        /// <summary>
        /// Creates a matrix that rotates around the y-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <returns>The created rotation matrix.</returns>
        public static Matrix RotationY(float angle)
        {
            Matrix result;
            RotationY(angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a matrix that rotates around the z-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="result">When the method completes, contains the created rotation matrix.</param>
        public static void RotationZ(float angle, out Matrix result)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            result = Matrix.Identity;
            result.M11 = cos;
            result.M12 = sin;
            result.M21 = -sin;
            result.M22 = cos;
        }

        /// <summary>
        /// Creates a matrix that rotates around the z-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <returns>The created rotation matrix.</returns>
        public static Matrix RotationZ(float angle)
        {
            Matrix result;
            RotationZ(angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a matrix that rotates around an arbitary axis.
        /// </summary>
        /// <param name="axis">The axis around which to rotate. This parameter is assumed to be normalized.</param>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="result">When the method completes, contains the created rotation matrix.</param>
        public static void RotationAxis(ref Vector3 axis, float angle, out Matrix result)
        {
            float x = axis.X;
            float y = axis.Y;
            float z = axis.Z;
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            float xx = x * x;
            float yy = y * y;
            float zz = z * z;
            float xy = x * y;
            float xz = x * z;
            float yz = y * z;

            result = Matrix.Identity;
            result.M11 = xx + (cos * (1.0f - xx));
            result.M12 = (xy - (cos * xy)) + (sin * z);
            result.M13 = (xz - (cos * xz)) - (sin * y);
            result.M21 = (xy - (cos * xy)) - (sin * z);
            result.M22 = yy + (cos * (1.0f - yy));
            result.M23 = (yz - (cos * yz)) + (sin * x);
            result.M31 = (xz - (cos * xz)) + (sin * y);
            result.M32 = (yz - (cos * yz)) - (sin * x);
            result.M33 = zz + (cos * (1.0f - zz));
        }

        /// <summary>
        /// Creates a matrix that rotates around an arbitary axis.
        /// </summary>
        /// <param name="axis">The axis around which to rotate. This parameter is assumed to be normalized.</param>
        /// <param name="angle">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <returns>The created rotation matrix.</returns>
        public static Matrix RotationAxis(Vector3 axis, float angle)
        {
            Matrix result;
            RotationAxis(ref axis, angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a rotation matrix from a quaternion.
        /// </summary>
        /// <param name="rotation">The quaternion to use to build the matrix.</param>
        /// <param name="result">The created rotation matrix.</param>
        public static void RotationQuaternion(ref Quaternion rotation, out Matrix result)
        {
            float xx = rotation.X * rotation.X;
            float yy = rotation.Y * rotation.Y;
            float zz = rotation.Z * rotation.Z;
            float xy = rotation.X * rotation.Y;
            float zw = rotation.Z * rotation.W;
            float zx = rotation.Z * rotation.X;
            float yw = rotation.Y * rotation.W;
            float yz = rotation.Y * rotation.Z;
            float xw = rotation.X * rotation.W;

            result = Matrix.Identity;
            result.M11 = 1.0f - (2.0f * (yy + zz));
            result.M12 = 2.0f * (xy + zw);
            result.M13 = 2.0f * (zx - yw);
            result.M21 = 2.0f * (xy - zw);
            result.M22 = 1.0f - (2.0f * (zz + xx));
            result.M23 = 2.0f * (yz + xw);
            result.M31 = 2.0f * (zx + yw);
            result.M32 = 2.0f * (yz - xw);
            result.M33 = 1.0f - (2.0f * (yy + xx));
        }

        /// <summary>
        /// Creates a matrix that contains both the X, Y and Z rotation, as well as scaling and translation. Note: This function is NOT thead safe.
        /// </summary>
        /// <param name="scaling">The scaling.</param>
        /// <param name="rotation">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="translation">The translation.</param>
        /// <param name="result">When the method completes, contains the created rotation matrix.</param>
        public static void Transformation(ref Vector3 scaling, ref Quaternion rotation, ref Vector3 translation, out Matrix result)
        {
            // Equivalent to:
            //result =
            //    Matrix.Scaling(scaling)
            //    *Matrix.RotationX(rotation.X)
            //    *Matrix.RotationY(rotation.Y)
            //    *Matrix.RotationZ(rotation.Z)
            //    *Matrix.Position(translation);

            // Rotation
            float xx = rotation.X * rotation.X;
            float yy = rotation.Y * rotation.Y;
            float zz = rotation.Z * rotation.Z;
            float xy = rotation.X * rotation.Y;
            float zw = rotation.Z * rotation.W;
            float zx = rotation.Z * rotation.X;
            float yw = rotation.Y * rotation.W;
            float yz = rotation.Y * rotation.Z;
            float xw = rotation.X * rotation.W;

            result.M11 = 1.0f - (2.0f * (yy + zz));
            result.M12 = 2.0f * (xy + zw);
            result.M13 = 2.0f * (zx - yw);
            result.M21 = 2.0f * (xy - zw);
            result.M22 = 1.0f - (2.0f * (zz + xx));
            result.M23 = 2.0f * (yz + xw);
            result.M31 = 2.0f * (zx + yw);
            result.M32 = 2.0f * (yz - xw);
            result.M33 = 1.0f - (2.0f * (yy + xx));

            // Position
            result.M41 = translation.X;
            result.M42 = translation.Y;
            result.M43 = translation.Z;

            // Scale
            if (scaling.X != 1.0f)
            {
                result.M11 *= scaling.X;
                result.M12 *= scaling.X;
                result.M13 *= scaling.X;
            }
            if (scaling.Y != 1.0f)
            {
                result.M21 *= scaling.Y;
                result.M22 *= scaling.Y;
                result.M23 *= scaling.Y;
            }
            if (scaling.Z != 1.0f)
            {
                result.M31 *= scaling.Z;
                result.M32 *= scaling.Z;
                result.M33 *= scaling.Z;
            }

            result.M14 = 0.0f;
            result.M24 = 0.0f;
            result.M34 = 0.0f;
            result.M44 = 1.0f;
        }

        /// <summary>
        /// Creates a rotation matrix from a quaternion.
        /// </summary>
        /// <param name="rotation">The quaternion to use to build the matrix.</param>
        /// <returns>The created rotation matrix.</returns>
        public static Matrix RotationQuaternion(Quaternion rotation)
        {
            Matrix result;
            RotationQuaternion(ref rotation, out result);
            return result;
        }

        /// <summary>
        /// Creates a rotation matrix with a specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Yaw around the y-axis, in radians.</param>
        /// <param name="pitch">Pitch around the x-axis, in radians.</param>
        /// <param name="roll">Roll around the z-axis, in radians.</param>
        /// <param name="result">When the method completes, contains the created rotation matrix.</param>
        public static void RotationYawPitchRoll(float yaw, float pitch, float roll, out Matrix result)
        {
            Quaternion quaternion = new Quaternion();
            Quaternion.RotationYawPitchRoll(yaw, pitch, roll, out quaternion);
            RotationQuaternion(ref quaternion, out result);
        }

        /// <summary>
        /// Creates a rotation matrix with a specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Yaw around the y-axis, in radians.</param>
        /// <param name="pitch">Pitch around the x-axis, in radians.</param>
        /// <param name="roll">Roll around the z-axis, in radians.</param>
        /// <returns>The created rotation matrix.</returns>
        public static Matrix RotationYawPitchRoll(float yaw, float pitch, float roll)
        {
            Matrix result;
            RotationYawPitchRoll(yaw, pitch, roll, out result);
            return result;
        }

        /// <summary>
        /// Creates a translation matrix using the specified offsets.
        /// </summary>
        /// <param name="value">The offset for all three coordinate planes.</param>
        /// <param name="result">When the method completes, contains the created translation matrix.</param>
        public static void Translation(ref Vector3 value, out Matrix result)
        {
            Translation(value.X, value.Y, value.Z, out result);
        }

        /// <summary>
        /// Creates a translation matrix using the specified offsets.
        /// </summary>
        /// <param name="value">The offset for all three coordinate planes.</param>
        /// <returns>The created translation matrix.</returns>
        public static Matrix Translation(Vector3 value)
        {
            Matrix result;
            Translation(ref value, out result);
            return result;
        }

        /// <summary>
        /// Creates a translation matrix using the specified offsets.
        /// </summary>
        /// <param name="x">X-coordinate offset.</param>
        /// <param name="y">Y-coordinate offset.</param>
        /// <param name="z">Z-coordinate offset.</param>
        /// <param name="result">When the method completes, contains the created translation matrix.</param>
        public static void Translation(float x, float y, float z, out Matrix result)
        {
            result = Matrix.Identity;
            result.M41 = x;
            result.M42 = y;
            result.M43 = z;
        }

        /// <summary>
        /// Creates a translation matrix using the specified offsets.
        /// </summary>
        /// <param name="x">X-coordinate offset.</param>
        /// <param name="y">Y-coordinate offset.</param>
        /// <param name="z">Z-coordinate offset.</param>
        /// <returns>The created translation matrix.</returns>
        public static Matrix Translation(float x, float y, float z)
        {
            Matrix result;
            Translation(x, y, z, out result);
            return result;
        }

        /// <summary>
        /// Creates a 3D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created affine transformation matrix.</param>
        public static void AffineTransformation(float scaling, ref Quaternion rotation, ref Vector3 translation, out Matrix result)
        {
            result = Scaling(scaling) * RotationQuaternion(rotation) * Translation(translation);
        }

        /// <summary>
        /// Creates a 3D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation matrix.</returns>
        public static Matrix AffineTransformation(float scaling, Quaternion rotation, Vector3 translation)
        {
            Matrix result;
            AffineTransformation(scaling, ref rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Creates a 3D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created affine transformation matrix.</param>
        public static void AffineTransformation(float scaling, ref Vector3 rotationCenter, ref Quaternion rotation, ref Vector3 translation, out Matrix result)
        {
            result = Scaling(scaling) * Translation(-rotationCenter) * RotationQuaternion(rotation) *
                Translation(rotationCenter) * Translation(translation);
        }

        /// <summary>
        /// Creates a 3D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation matrix.</returns>
        public static Matrix AffineTransformation(float scaling, Vector3 rotationCenter, Quaternion rotation, Vector3 translation)
        {
            Matrix result;
            AffineTransformation(scaling, ref rotationCenter, ref rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Creates a 2D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created affine transformation matrix.</param>
        public static void AffineTransformation2D(float scaling, float rotation, ref Vector2 translation, out Matrix result)
        {
            result = Scaling(scaling, scaling, 1.0f) * RotationZ(rotation) * Translation((Vector3)translation);
        }

        /// <summary>
        /// Creates a 2D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation matrix.</returns>
        public static Matrix AffineTransformation2D(float scaling, float rotation, Vector2 translation)
        {
            Matrix result;
            AffineTransformation2D(scaling, rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Creates a 2D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created affine transformation matrix.</param>
        public static void AffineTransformation2D(float scaling, ref Vector2 rotationCenter, float rotation, ref Vector2 translation, out Matrix result)
        {
            result = Scaling(scaling, scaling, 1.0f) * Translation((Vector3)(-rotationCenter)) * RotationZ(rotation) *
                Translation((Vector3)rotationCenter) * Translation((Vector3)translation);
        }

        /// <summary>
        /// Creates a 2D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation matrix.</returns>
        public static Matrix AffineTransformation2D(float scaling, Vector2 rotationCenter, float rotation, Vector2 translation)
        {
            Matrix result;
            AffineTransformation2D(scaling, ref rotationCenter, rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Creates a transformation matrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created transformation matrix.</param>
        public static void Transformation(ref Vector3 scalingCenter, ref Quaternion scalingRotation, ref Vector3 scaling, ref Vector3 rotationCenter, ref Quaternion rotation, ref Vector3 translation, out Matrix result)
        {
            Matrix sr = RotationQuaternion(scalingRotation);

            result = Translation(-scalingCenter) * Transpose(sr) * Scaling(scaling) * sr * Translation(scalingCenter) * Translation(-rotationCenter) *
                RotationQuaternion(rotation) * Translation(rotationCenter) * Translation(translation);
        }

        /// <summary>
        /// Creates a transformation matrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created transformation matrix.</returns>
        public static Matrix Transformation(Vector3 scalingCenter, Quaternion scalingRotation, Vector3 scaling, Vector3 rotationCenter, Quaternion rotation, Vector3 translation)
        {
            Matrix result;
            Transformation(ref scalingCenter, ref scalingRotation, ref scaling, ref rotationCenter, ref rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Creates a 2D transformation matrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <param name="result">When the method completes, contains the created transformation matrix.</param>
        public static void Transformation2D(ref Vector2 scalingCenter, float scalingRotation, ref Vector2 scaling, ref Vector2 rotationCenter, float rotation, ref Vector2 translation, out Matrix result)
        {
            result = Translation((Vector3)(-scalingCenter)) * RotationZ(-scalingRotation) * Scaling((Vector3)scaling) * RotationZ(scalingRotation) * Translation((Vector3)scalingCenter) *
                Translation((Vector3)(-rotationCenter)) * RotationZ(rotation) * Translation((Vector3)rotationCenter) * Translation((Vector3)translation);

            result.M33 = 1f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a 2D transformation matrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created transformation matrix.</returns>
        public static Matrix Transformation2D(Vector2 scalingCenter, float scalingRotation, Vector2 scaling, Vector2 rotationCenter, float rotation, Vector2 translation)
        {
            Matrix result;
            Transformation2D(ref scalingCenter, scalingRotation, ref scaling, ref rotationCenter, rotation, ref translation, out result);
            return result;
        }

        /// <summary>
        /// Copies a nxm matrix to this instance.
        /// </summary>
        /// <param name="src">The source matrix.</param>
        /// <param name="columns">The number of columns.</param>
        /// <param name="rows">The number of rows.</param>
        public unsafe void CopyMatrixFrom(float* src, int columns, int rows)
        {
            fixed (void* pDest = &this)
            {
                var dest = (float*)pDest;
                for (int i = 0; i < rows; ++i)
                {
                    for (int j = 0; j < columns; ++j)
                    {
                        dest[j] = src[j];
                    }
                    dest += 4;
                    src += rows;
                }
            }
        }

        /// <summary>
        /// Transposes a nmx matrix to this instance.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <param name="columns">The columns.</param>
        /// <param name="rows">The rows.</param>
        public unsafe void TransposeMatrixFrom(float* src, int columns, int rows)
        {
            fixed (void* pDest = &this)
            {
                var dest = (float*)pDest;
                for (int i = 0; i < rows; ++i)
                {
                    int sourceIndex = i;
                    for (int j = 0; j < columns; ++j)
                    {
                        dest[j] = src[sourceIndex];
                        sourceIndex += rows;
                    }
                    dest += 4;
                }
            }
        }

        /// <summary>
        /// Adds two matrices.
        /// </summary>
        /// <param name="left">The first matrix to add.</param>
        /// <param name="right">The second matrix to add.</param>
        /// <returns>The sum of the two matrices.</returns>
        public static Matrix operator +(Matrix left, Matrix right)
        {
            Matrix result;
            Add(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Assert a matrix (return it unchanged).
        /// </summary>
        /// <param name="value">The matrix to assert (unchange).</param>
        /// <returns>The asserted (unchanged) matrix.</returns>
        public static Matrix operator +(Matrix value)
        {
            return value;
        }

        /// <summary>
        /// Subtracts two matrices.
        /// </summary>
        /// <param name="left">The first matrix to subtract.</param>
        /// <param name="right">The second matrix to subtract.</param>
        /// <returns>The difference between the two matrices.</returns>
        public static Matrix operator -(Matrix left, Matrix right)
        {
            Matrix result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Negates a matrix.
        /// </summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        public static Matrix operator -(Matrix value)
        {
            Matrix result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Scales a matrix by a given value.
        /// </summary>
        /// <param name="right">The matrix to scale.</param>
        /// <param name="left">The amount by which to scale.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix operator *(float left, Matrix right)
        {
            Matrix result;
            Multiply(ref right, left, out result);
            return result;
        }

        /// <summary>
        /// Scales a matrix by a given value.
        /// </summary>
        /// <param name="left">The matrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix operator *(Matrix left, float right)
        {
            Matrix result;
            Multiply(ref left, right, out result);
            return result;
        }

        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="left">The first matrix to multiply.</param>
        /// <param name="right">The second matrix to multiply.</param>
        /// <returns>The product of the two matrices.</returns>
        public static Matrix operator *(Matrix left, Matrix right)
        {
            Matrix result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Scales a matrix by a given value.
        /// </summary>
        /// <param name="left">The matrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix operator /(Matrix left, float right)
        {
            Matrix result;
            Divide(ref left, right, out result);
            return result;
        }

        /// <summary>
        /// Divides two matrices.
        /// </summary>
        /// <param name="left">The first matrix to divide.</param>
        /// <param name="right">The second matrix to divide.</param>
        /// <returns>The quotient of the two matrices.</returns>
        public static Matrix operator /(Matrix left, Matrix right)
        {
            Matrix result;
            Divide(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Matrix left, Matrix right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Matrix left, Matrix right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "[M11:{0} M12:{1} M13:{2} M14:{3}] [M21:{4} M22:{5} M23:{6} M24:{7}] [M31:{8} M32:{9} M33:{10} M34:{11}] [M41:{12} M42:{13} M43:{14} M44:{15}]",
                M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(string format)
        {
            if (format == null)
                return ToString();

            return string.Format(format, CultureInfo.CurrentCulture, "[M11:{0} M12:{1} M13:{2} M14:{3}] [M21:{4} M22:{5} M23:{6} M24:{7}] [M31:{8} M32:{9} M33:{10} M34:{11}] [M41:{12} M42:{13} M43:{14} M44:{15}]",
                M11.ToString(format, CultureInfo.CurrentCulture), M12.ToString(format, CultureInfo.CurrentCulture), M13.ToString(format, CultureInfo.CurrentCulture), M14.ToString(format, CultureInfo.CurrentCulture),
                M21.ToString(format, CultureInfo.CurrentCulture), M22.ToString(format, CultureInfo.CurrentCulture), M23.ToString(format, CultureInfo.CurrentCulture), M24.ToString(format, CultureInfo.CurrentCulture),
                M31.ToString(format, CultureInfo.CurrentCulture), M32.ToString(format, CultureInfo.CurrentCulture), M33.ToString(format, CultureInfo.CurrentCulture), M34.ToString(format, CultureInfo.CurrentCulture),
                M41.ToString(format, CultureInfo.CurrentCulture), M42.ToString(format, CultureInfo.CurrentCulture), M43.ToString(format, CultureInfo.CurrentCulture), M44.ToString(format, CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "[M11:{0} M12:{1} M13:{2} M14:{3}] [M21:{4} M22:{5} M23:{6} M24:{7}] [M31:{8} M32:{9} M33:{10} M34:{11}] [M41:{12} M42:{13} M43:{14} M44:{15}]",
                M11.ToString(formatProvider), M12.ToString(formatProvider), M13.ToString(formatProvider), M14.ToString(formatProvider),
                M21.ToString(formatProvider), M22.ToString(formatProvider), M23.ToString(formatProvider), M24.ToString(formatProvider),
                M31.ToString(formatProvider), M32.ToString(formatProvider), M33.ToString(formatProvider), M34.ToString(formatProvider),
                M41.ToString(formatProvider), M42.ToString(formatProvider), M43.ToString(formatProvider), M44.ToString(formatProvider));
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return ToString(formatProvider);

            return string.Format(format, formatProvider, "[M11:{0} M12:{1} M13:{2} M14:{3}] [M21:{4} M22:{5} M23:{6} M24:{7}] [M31:{8} M32:{9} M33:{10} M34:{11}] [M41:{12} M42:{13} M43:{14} M44:{15}]",
                M11.ToString(format, formatProvider), M12.ToString(format, formatProvider), M13.ToString(format, formatProvider), M14.ToString(format, formatProvider),
                M21.ToString(format, formatProvider), M22.ToString(format, formatProvider), M23.ToString(format, formatProvider), M24.ToString(format, formatProvider),
                M31.ToString(format, formatProvider), M32.ToString(format, formatProvider), M33.ToString(format, formatProvider), M34.ToString(format, formatProvider),
                M41.ToString(format, formatProvider), M42.ToString(format, formatProvider), M43.ToString(format, formatProvider), M44.ToString(format, formatProvider));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() + M14.GetHashCode() +
               M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() + M24.GetHashCode() +
               M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode() + M34.GetHashCode() +
               M41.GetHashCode() + M42.GetHashCode() + M43.GetHashCode() + M44.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="Xenko.Core.Mathematics.Matrix"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Xenko.Core.Mathematics.Matrix"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Xenko.Core.Mathematics.Matrix"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Matrix other)
        {
            return (Math.Abs(other.M11 - M11) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M12 - M12) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M13 - M13) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M14 - M14) < MathUtil.ZeroTolerance &&

                Math.Abs(other.M21 - M21) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M22 - M22) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M23 - M23) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M24 - M24) < MathUtil.ZeroTolerance &&

                Math.Abs(other.M31 - M31) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M32 - M32) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M33 - M33) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M34 - M34) < MathUtil.ZeroTolerance &&

                Math.Abs(other.M41 - M41) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M42 - M42) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M43 - M43) < MathUtil.ZeroTolerance &&
                Math.Abs(other.M44 - M44) < MathUtil.ZeroTolerance);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object value)
        {
            if (value == null)
                return false;

            if (value.GetType() != GetType())
                return false;

            return Equals((Matrix)value);
        }

#if SlimDX1xInterop
        /// <summary>
        /// Performs an implicit conversion from <see cref="Xenko.Core.Mathematics.Matrix"/> to <see cref="SlimDX.Matrix"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator SlimDX.Matrix(Matrix value)
        {
            return new SlimDX.Matrix()
            {
                M11 = value.M11, M12 = value.M12, M13 = value.M13, M14 = value.M14,
                M21 = value.M21, M22 = value.M22, M23 = value.M23, M24 = value.M24,
                M31 = value.M31, M32 = value.M32, M33 = value.M33, M34 = value.M34,
                M41 = value.M41, M42 = value.M42, M43 = value.M43, M44 = value.M44
            };
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SlimDX.Matrix"/> to <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Matrix(SlimDX.Matrix value)
        {
            return new Matrix()
            {
                M11 = value.M11, M12 = value.M12, M13 = value.M13, M14 = value.M14,
                M21 = value.M21, M22 = value.M22, M23 = value.M23, M24 = value.M24,
                M31 = value.M31, M32 = value.M32, M33 = value.M33, M34 = value.M34,
                M41 = value.M41, M42 = value.M42, M43 = value.M43, M44 = value.M44
            };
        }
#endif

#if WPFInterop
        /// <summary>
        /// Performs an implicit conversion from <see cref="Xenko.Core.Mathematics.Matrix"/> to <see cref="System.Windows.Media.Media3D.Matrix3D"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator System.Windows.Media.Media3D.Matrix3D(Matrix value)
        {
            return new System.Windows.Media.Media3D.Matrix3D()
            {
                M11 = value.M11, M12 = value.M12, M13 = value.M13, M14 = value.M14,
                M21 = value.M21, M22 = value.M22, M23 = value.M23, M24 = value.M24,
                M31 = value.M31, M32 = value.M32, M33 = value.M33, M34 = value.M34,
                OffsetX = value.M41, OffsetY = value.M42, OffsetZ = value.M43, M44 = value.M44
            };
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Windows.Media.Media3D.Matrix3D"/> to <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Matrix(System.Windows.Media.Media3D.Matrix3D value)
        {
            return new Matrix()
            {
                M11 = (float)value.M11, M12 = (float)value.M12, M13 = (float)value.M13, M14 = (float)value.M14,
                M21 = (float)value.M21, M22 = (float)value.M22, M23 = (float)value.M23, M24 = (float)value.M24,
                M31 = (float)value.M31, M32 = (float)value.M32, M33 = (float)value.M33, M34 = (float)value.M34,
                M41 = (float)value.OffsetX, M42 = (float)value.OffsetY, M43 = (float)value.OffsetZ, M44 = (float)value.M44
            };
        }
#endif

#if XnaInterop
        /// <summary>
        /// Performs an implicit conversion from <see cref="Xenko.Core.Mathematics.Matrix"/> to <see cref="Microsoft.Xna.Framework.Matrix"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Microsoft.Xna.Framework.Matrix(Matrix value)
        {
            return new Microsoft.Xna.Framework.Matrix()
            {
                M11 = value.M11, M12 = value.M12, M13 = value.M13, M14 = value.M14,
                M21 = value.M21, M22 = value.M22, M23 = value.M23, M24 = value.M24,
                M31 = value.M31, M32 = value.M32, M33 = value.M33, M34 = value.M34,
                M41 = value.M41, M42 = value.M42, M43 = value.M43, M44 = value.M44
            };
        }

                /// <summary>
        /// Performs an implicit conversion from <see cref="Microsoft.Xna.Framework.Matrix"/> to <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Matrix(Microsoft.Xna.Framework.Matrix value)
        {
            return new Matrix()
            {
                M11 = value.M11, M12 = value.M12, M13 = value.M13, M14 = value.M14,
                M21 = value.M21, M22 = value.M22, M23 = value.M23, M24 = value.M24,
                M31 = value.M31, M32 = value.M32, M33 = value.M33, M34 = value.M34,
                M41 = value.M41, M42 = value.M42, M43 = value.M43, M44 = value.M44
            };
        }
#endif
    }
}
