// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Animations
{
    [DataContract("ComputeSeparateCurveVector4")]
    [Display("4 Channels")]
    public class ComputeSeparateCurveVector4 : IComputeCurve<Vector4>
    {
        [DataMember(10)]
        [NotNull]
        [Display("X")]
        public IComputeCurve<float> X
        {
            get { return xValue; }
            set
            {
                xValue = value;
                hasChanged = true;
            }
        }

        [DataMember(20)]
        [NotNull]
        [Display("Y")]
        public IComputeCurve<float> Y
        {
            get { return yValue; }
            set
            {
                yValue = value;
                hasChanged = true;
            }
        }

        [DataMember(30)]
        [NotNull]
        [Display("Z")]
        public IComputeCurve<float> Z
        {
            get { return zValue; }
            set
            {
                zValue = value;
                hasChanged = true;
            }
        }

        [DataMember(40)]
        [NotNull]
        [Display("W")]
        public IComputeCurve<float> W
        {
            get { return wValue; }
            set
            {
                wValue = value;
                hasChanged = true;
            }
        }

        public Vector4 Evaluate(float t)
        {
            return new Vector4(X.Evaluate(t), Y.Evaluate(t), Z.Evaluate(t), W.Evaluate(t));            
        }

        private bool hasChanged = true;
        private IComputeCurve<float> xValue = new ComputeConstCurveFloat();
        private IComputeCurve<float> yValue = new ComputeConstCurveFloat();
        private IComputeCurve<float> zValue = new ComputeConstCurveFloat();
        private IComputeCurve<float> wValue = new ComputeConstCurveFloat();

        /// <inheritdoc/>
        public bool UpdateChanges()
        {
            if (hasChanged)
            {
                hasChanged = false;
                return true;
            }

            return (X?.UpdateChanges() ?? false) || (Y?.UpdateChanges() ?? false) || (Z?.UpdateChanges() ?? false) || (W?.UpdateChanges() ?? false);
        }
    }
}
