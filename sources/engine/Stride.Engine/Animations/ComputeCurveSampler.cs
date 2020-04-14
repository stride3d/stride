// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Animations
{
    /// <summary>
    /// Base interface for curve based compute value nodes.
    /// </summary>
    [DataContract(Inherited = true)]
    public abstract class ComputeCurveSampler<T> where T : struct
    {
        protected IComputeCurve<T> curve;

        [NotNull]
        [DataMember(10)]
        [Display("Curve")]
        public IComputeCurve<T> Curve
        {
            get
            {
                return curve;
            }

            set
            {
                curve = value;
                hasChanged = true;
            }
        }

        protected ComputeCurveSampler()
        {
            bakedArray = new T[BakedArraySize];
            BakeData();
        }

        /// <summary>
        /// Evaluates the compute curve's value at the specified location, usually in the [0 .. 1] range
        /// </summary>
        /// <param name="location">Location to sample at</param>
        /// <returns>Sampled value</returns>
        public T Evaluate(float t)
        {
            var indexLocation = t * (BakedArraySize - 1);
            var index = (int)indexLocation;
            var lerpValue = indexLocation - index;

            T result;
            var thisIndex = (int)Math.Max(index, 0);
            var nextIndex = (int)Math.Min(index + 1, BakedArraySize - 1);
            Linear(ref bakedArray[thisIndex], ref bakedArray[nextIndex], lerpValue, out result);
            return result;
        }

        /// <summary>
        /// Interface for linera interpolation between two data values
        /// </summary>
        /// <param name="value1">Left value</param>
        /// <param name="value2">Right value</param>
        /// <param name="t">Lerp amount between 0 and 1</param>
        /// <param name="result">The interpolated result of linearLerp(L, R, t)</param>
        public abstract void Linear(ref T value1, ref T value2, float t, out T result);

        // TODO Maybe make it variable length/density
        private const uint BakedArraySize = 32;

        /// <summary>
        /// Data in this sampler can be baked to allow faster sampling
        /// </summary>
        [DataMemberIgnore]
        private T[] bakedArray;

        /// <summary>
        /// Bakes the sampled data in a fixed size array for faster access
        /// </summary>
        private void BakeData()
        {
            if (curve == null)
            {
                var emptyValue = new T();
                for (var i = 0; i < BakedArraySize; i++)
                {
                    bakedArray[i] = emptyValue;
                }

                return;
            }

            curve.UpdateChanges();

            for (var i = 0; i < BakedArraySize; i++)
            {
                var t = i / (float)(BakedArraySize - 1);
                bakedArray[i] = curve.Evaluate(t);
            }
        }

        private bool hasChanged = true;

        /// <inheritdoc/>
        public bool UpdateChanges()
        {
            if (hasChanged || (curve?.UpdateChanges() ?? false))
            {
                BakeData();
                hasChanged = false;
                return true;
            }

            return false;
        }
    }
}
