// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Annotations;

namespace Xenko.Animations
{
    /// <summary>
    /// A node which describes a function over t value for a compute curve
    /// </summary>
    /// <typeparam name="T">Sampled data's type</typeparam>
    [DataContract(Inherited = true)]
    [Display("Function")]
    [InlineProperty]
    public abstract class ComputeFunctionCurve<T> : IComputeCurve<T> where T : struct
    {
        [DataMember(10)]
        [Display("Phase Shift")]
        public float PhaseShift
        {
            get { return phaseShift; }
            set
            {
                phaseShift = value;
                hasChanged = true;
            }
        }
        private float phaseShift = 0f;

        [DataMember(12)]
        [Display("Period")]
        public float Period
        {
            get { return period; }
            set
            {
                period = value;
                hasChanged = true;
            }
        }
        private float period = 1f;

        [DataMember(14)]
        [Display("Amplitude")]
        public float Amplitude
        {
            get { return amplitude; }
            set
            {
                amplitude = value;
                hasChanged = true;
            }
        }
        private float amplitude = 1f;

        private bool hasChanged = true;
        /// <inheritdoc/>
        public bool UpdateChanges()
        {
            if (hasChanged)
            {
                hasChanged = false;
                return true;
            }

            return false;
        }

        protected abstract T GetElementFrom(float value);

        /// <inheritdoc/>
        public T Evaluate(float location)
        {
            var value = amplitude * (float)Math.Sin(2 * Math.PI * (phaseShift + location / period));
            return GetElementFrom(value);
        }
    }
}
