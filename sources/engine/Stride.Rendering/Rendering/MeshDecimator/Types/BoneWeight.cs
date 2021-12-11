using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace Stride.Rendering.Rendering.MeshDecimator.Types
{
    public struct BoneWeight : IEquatable<BoneWeight>
    {
        #region Fields
        /// <summary>
        /// The first bone index.
        /// </summary>
        public int boneIndex0;
        /// <summary>
        /// The second bone index.
        /// </summary>
        public int boneIndex1;
        /// <summary>
        /// The third bone index.
        /// </summary>
        public int boneIndex2;
        /// <summary>
        /// The fourth bone index.
        /// </summary>
        public int boneIndex3;

        /// <summary>
        /// The first bone weight.
        /// </summary>
        public float boneWeight0;
        /// <summary>
        /// The second bone weight.
        /// </summary>
        public float boneWeight1;
        /// <summary>
        /// The third bone weight.
        /// </summary>
        public float boneWeight2;
        /// <summary>
        /// The fourth bone weight.
        /// </summary>
        public float boneWeight3;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new bone weight.
        /// </summary>
        /// <param name="boneIndex0">The first bone index.</param>
        /// <param name="boneIndex1">The second bone index.</param>
        /// <param name="boneIndex2">The third bone index.</param>
        /// <param name="boneIndex3">The fourth bone index.</param>
        /// <param name="boneWeight0">The first bone weight.</param>
        /// <param name="boneWeight1">The second bone weight.</param>
        /// <param name="boneWeight2">The third bone weight.</param>
        /// <param name="boneWeight3">The fourth bone weight.</param>
        public BoneWeight(int boneIndex0, int boneIndex1, int boneIndex2, int boneIndex3, float boneWeight0, float boneWeight1, float boneWeight2, float boneWeight3)
        {
            this.boneIndex0 = boneIndex0;
            this.boneIndex1 = boneIndex1;
            this.boneIndex2 = boneIndex2;
            this.boneIndex3 = boneIndex3;

            this.boneWeight0 = boneWeight0;
            this.boneWeight1 = boneWeight1;
            this.boneWeight2 = boneWeight2;
            this.boneWeight3 = boneWeight3;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Returns if two bone weights equals eachother.
        /// </summary>
        /// <param name="lhs">The left hand side bone weight.</param>
        /// <param name="rhs">The right hand side bone weight.</param>
        /// <returns>If equals.</returns>
        public static bool operator ==(BoneWeight lhs, BoneWeight rhs)
        {
            return (lhs.boneIndex0 == rhs.boneIndex0 && lhs.boneIndex1 == rhs.boneIndex1 && lhs.boneIndex2 == rhs.boneIndex2 && lhs.boneIndex3 == rhs.boneIndex3 &&
                new Vector4(lhs.boneWeight0, lhs.boneWeight1, lhs.boneWeight2, lhs.boneWeight3) == new Vector4(rhs.boneWeight0, rhs.boneWeight1, rhs.boneWeight2, rhs.boneWeight3));
        }

        /// <summary>
        /// Returns if two bone weights don't equal eachother.
        /// </summary>
        /// <param name="lhs">The left hand side bone weight.</param>
        /// <param name="rhs">The right hand side bone weight.</param>
        /// <returns>If not equals.</returns>
        public static bool operator !=(BoneWeight lhs, BoneWeight rhs)
        {
            return !(lhs == rhs);
        }
        #endregion

        #region Private Methods
        private void MergeBoneWeight(int boneIndex, float weight)
        {
            if (boneIndex == boneIndex0)
            {
                boneWeight0 = (boneWeight0 + weight) * 0.5f;
            }
            else if (boneIndex == boneIndex1)
            {
                boneWeight1 = (boneWeight1 + weight) * 0.5f;
            }
            else if (boneIndex == boneIndex2)
            {
                boneWeight2 = (boneWeight2 + weight) * 0.5f;
            }
            else if (boneIndex == boneIndex3)
            {
                boneWeight3 = (boneWeight3 + weight) * 0.5f;
            }
            else if (boneWeight0 == 0f)
            {
                boneIndex0 = boneIndex;
                boneWeight0 = weight;
            }
            else if (boneWeight1 == 0f)
            {
                boneIndex1 = boneIndex;
                boneWeight1 = weight;
            }
            else if (boneWeight2 == 0f)
            {
                boneIndex2 = boneIndex;
                boneWeight2 = weight;
            }
            else if (boneWeight3 == 0f)
            {
                boneIndex3 = boneIndex;
                boneWeight3 = weight;
            }
            Normalize();
        }

        private void Normalize()
        {
            float mag = (float)System.Math.Sqrt(boneWeight0 * boneWeight0 + boneWeight1 * boneWeight1 + boneWeight2 * boneWeight2 + boneWeight3 * boneWeight3);
            if (mag > float.Epsilon)
            {
                boneWeight0 /= mag;
                boneWeight1 /= mag;
                boneWeight2 /= mag;
                boneWeight3 /= mag;
            }
            else
            {
                boneWeight0 = boneWeight1 = boneWeight2 = boneWeight3 = 0f;
            }
        }
        #endregion

        #region Public Methods
        #region Object
        /// <summary>
        /// Returns a hash code for this vector.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return boneIndex0.GetHashCode() ^ boneIndex1.GetHashCode() << 2 ^ boneIndex2.GetHashCode() >> 2 ^ boneIndex3.GetHashCode() >>
                1 ^ boneWeight0.GetHashCode() << 5 ^ boneWeight1.GetHashCode() << 4 ^ boneWeight2.GetHashCode() >> 4 ^ boneWeight3.GetHashCode() >> 3;
        }

        /// <summary>
        /// Returns if this bone weight is equal to another object.
        /// </summary>
        /// <param name="obj">The other object to compare to.</param>
        /// <returns>If equals.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is BoneWeight))
            {
                return false;
            }
            BoneWeight other = (BoneWeight)obj;
            return (boneIndex0 == other.boneIndex0 && boneIndex1 == other.boneIndex1 && boneIndex2 == other.boneIndex2 && boneIndex3 == other.boneIndex3 &&
                boneWeight0 == other.boneWeight0 && boneWeight1 == other.boneWeight1 && boneWeight2 == other.boneWeight2 && boneWeight3 == other.boneWeight3);
        }

        /// <summary>
        /// Returns if this bone weight is equal to another one.
        /// </summary>
        /// <param name="other">The other bone weight to compare to.</param>
        /// <returns>If equals.</returns>
        public bool Equals(BoneWeight other)
        {
            return (boneIndex0 == other.boneIndex0 && boneIndex1 == other.boneIndex1 && boneIndex2 == other.boneIndex2 && boneIndex3 == other.boneIndex3 &&
                boneWeight0 == other.boneWeight0 && boneWeight1 == other.boneWeight1 && boneWeight2 == other.boneWeight2 && boneWeight3 == other.boneWeight3);
        }

        /// <summary>
        /// Returns a nicely formatted string for this bone weight.
        /// </summary>
        /// <returns>The string.</returns>
        public override string ToString()
        {
            return string.Format("({0}:{4:F1}, {1}:{5:F1}, {2}:{6:F1}, {3}:{7:F1})",
                boneIndex0, boneIndex1, boneIndex2, boneIndex3, boneWeight0, boneWeight1, boneWeight2, boneWeight3);
        }
        #endregion

        #region Static
        /// <summary>
        /// Merges two bone weights and stores the merged result in the first parameter.
        /// </summary>
        /// <param name="a">The first bone weight, also stores result.</param>
        /// <param name="b">The second bone weight.</param>
        public static void Merge(ref BoneWeight a, ref BoneWeight b)
        {
            if (b.boneWeight0 > 0f) a.MergeBoneWeight(b.boneIndex0, b.boneWeight0);
            if (b.boneWeight1 > 0f) a.MergeBoneWeight(b.boneIndex1, b.boneWeight1);
            if (b.boneWeight2 > 0f) a.MergeBoneWeight(b.boneIndex2, b.boneWeight2);
            if (b.boneWeight3 > 0f) a.MergeBoneWeight(b.boneIndex3, b.boneWeight3);
        }
        #endregion
        #endregion
    }
}
