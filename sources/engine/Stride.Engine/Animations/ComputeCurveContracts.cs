// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Animations
{
    #region Vector4
    /// <summary>
    /// Sampler container for Vector4 data type
    /// </summary>
    [DataContract("ComputeCurveSamplerVector4")]
    [Display("Sampler Vector4")]
    public class ComputeCurveSamplerVector4 : ComputeCurveSampler<Vector4>
    {
        public ComputeCurveSamplerVector4()
        {
            curve = new ComputeAnimationCurveVector4();
        }

        /// <inheritdoc/>
        public override void Linear(ref Vector4 value1, ref Vector4 value2, float t, out Vector4 result)
        {
            Interpolator.Vector4.Linear(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Constant Vector4 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeConstCurveVector4")]
    [Display("Constant")]
    public class ComputeConstCurveVector4 : ComputeConstCurve<Vector4> { }

    /// <summary>
    /// Function Vector4 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeFunctionCurveVector4")]
    [Display("Function")]
    public class ComputeFunctionCurveVector4 : ComputeFunctionCurve<Vector4>
    {
        protected override Vector4 GetElementFrom(float value) { return new Vector4(value, value, value, value); }
    }

    /// <summary>
    /// Constant Vector4 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeAnimationCurveVector4")]
    [Display("Animation", Expand = ExpandRule.Never)]
    public class ComputeAnimationCurveVector4 : ComputeAnimationCurve<Vector4>
    {
        /// <inheritdoc/>
        public override void Cubic(ref Vector4 value1, ref Vector4 value2, ref Vector4 value3, ref Vector4 value4, float t, out Vector4 result)
        {
            Interpolator.Vector4.Cubic(ref value1, ref value2, ref value3, ref value4, t, out result);
        }

        /// <inheritdoc/>
        public override void Linear(ref Vector4 value1, ref Vector4 value2, float t, out Vector4 result)
        {
            Interpolator.Vector4.Linear(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Binary operator Vector4 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeBinaryCurveVector4")]
    [Display("Binary Operation")]
    public class ComputeBinaryCurveVector4 : ComputeBinaryCurve<Vector4>
    {
        /// <inheritdoc/>
        protected override Vector4 Add(Vector4 a, Vector4 b)
        {
            return a + b;
        }

        /// <inheritdoc/>
        protected override Vector4 Subtract(Vector4 a, Vector4 b)
        {
            return a - b;
        }

        /// <inheritdoc/>
        protected override Vector4 Multiply(Vector4 a, Vector4 b)
        {
            return a * b;
        }
    }

    #endregion

    #region Vector3
    /// <summary>
    /// Sampler container for Vector3 data type
    /// </summary>
    [DataContract("ComputeCurveSamplerVector3")]
    [Display("Sampler Vector3")]
    public class ComputeCurveSamplerVector3 : ComputeCurveSampler<Vector3>
    {
        public ComputeCurveSamplerVector3()
        {
            curve = new ComputeAnimationCurveVector3();
        }

        /// <inheritdoc/>
        public override void Linear(ref Vector3 value1, ref Vector3 value2, float t, out Vector3 result)
        {
            Interpolator.Vector3.Linear(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Constant Vector3 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeConstCurveVector3")]
    [Display("Constant")]
    public class ComputeConstCurveVector3 : ComputeConstCurve<Vector3> { }

    /// <summary>
    /// Function Vector3 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeFunctionCurveVector3")]
    [Display("Function")]
    public class ComputeFunctionCurveVector3 : ComputeFunctionCurve<Vector3>
    {
        protected override Vector3 GetElementFrom(float value) { return new Vector3(value, value, value); }
    }

    /// <summary>
    /// Constant Vector3 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeAnimationCurveVector3")]
    [Display("Animation", Expand = ExpandRule.Never)]
    public class ComputeAnimationCurveVector3 : ComputeAnimationCurve<Vector3>
    {
        /// <inheritdoc/>
        public override void Cubic(ref Vector3 value1, ref Vector3 value2, ref Vector3 value3, ref Vector3 value4, float t, out Vector3 result)
        {
            Interpolator.Vector3.Cubic(ref value1, ref value2, ref value3, ref value4, t, out result);
        }

        /// <inheritdoc/>
        public override void Linear(ref Vector3 value1, ref Vector3 value2, float t, out Vector3 result)
        {
            Interpolator.Vector3.Linear(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Binary operator Vector3 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeBinaryCurveVector3")]
    [Display("Binary Operation")]
    public class ComputeBinaryCurveVector3 : ComputeBinaryCurve<Vector3>
    {
        /// <inheritdoc/>
        protected override Vector3 Add(Vector3 a, Vector3 b)
        {
            return a + b;
        }

        /// <inheritdoc/>
        protected override Vector3 Subtract(Vector3 a, Vector3 b)
        {
            return a - b;
        }

        /// <inheritdoc/>
        protected override Vector3 Multiply(Vector3 a, Vector3 b)
        {
            return a * b;
        }
    }

    #endregion

    #region Vector2
    /// <summary>
    /// Sampler container for Vector2 data type
    /// </summary>
    [DataContract("ComputeCurveSamplerVector2")]
    [Display("Sampler Vector2")]
    public class ComputeCurveSamplerVector2 : ComputeCurveSampler<Vector2>
    {
        public ComputeCurveSamplerVector2()
        {
            curve = new ComputeAnimationCurveVector2();
        }

        /// <inheritdoc/>
        public override void Linear(ref Vector2 value1, ref Vector2 value2, float t, out Vector2 result)
        {
            Interpolator.Vector2.Linear(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Constant Vector2 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeConstCurveVector2")]
    [Display("Constant")]
    public class ComputeConstCurveVector2 : ComputeConstCurve<Vector2> { }

    /// <summary>
    /// Function Vector2 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeFunctionCurveVector2")]
    [Display("Function")]
    public class ComputeFunctionCurveVector2 : ComputeFunctionCurve<Vector2>
    {
        protected override Vector2 GetElementFrom(float value) { return new Vector2(value, value); }
    }

    /// <summary>
    /// Constant Vector2 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeAnimationCurveVector2")]
    [Display("Animation", Expand = ExpandRule.Never)]
    public class ComputeAnimationCurveVector2 : ComputeAnimationCurve<Vector2>
    {
        /// <inheritdoc/>
        public override void Cubic(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, ref Vector2 value4, float t, out Vector2 result)
        {
            Interpolator.Vector2.Cubic(ref value1, ref value2, ref value3, ref value4, t, out result);
        }

        /// <inheritdoc/>
        public override void Linear(ref Vector2 value1, ref Vector2 value2, float t, out Vector2 result)
        {
            Interpolator.Vector2.Linear(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Binary operator Vector2 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeBinaryCurveVector2")]
    [Display("Binary Operation")]
    public class ComputeBinaryCurveVector2 : ComputeBinaryCurve<Vector2>
    {
        /// <inheritdoc/>
        protected override Vector2 Add(Vector2 a, Vector2 b)
        {
            return a + b;
        }

        /// <inheritdoc/>
        protected override Vector2 Subtract(Vector2 a, Vector2 b)
        {
            return a - b;
        }

        /// <inheritdoc/>
        protected override Vector2 Multiply(Vector2 a, Vector2 b)
        {
            return a * b;
        }
    }

    #endregion

    #region Float
    /// <summary>
    /// Sampler container for float data type
    /// </summary>
    [DataContract("ComputeCurveSamplerFloat")]
    [Display("Sampler Float")]
    public class ComputeCurveSamplerFloat : ComputeCurveSampler<float>
    {
        public ComputeCurveSamplerFloat()
        {
            curve = new ComputeAnimationCurveFloat();
        }

        /// <inheritdoc/>
        public override void Linear(ref float value1, ref float value2, float t, out float result)
        {
            result = value1 + (value2 - value1) * t;
        }
    }

    /// <summary>
    /// Constant float value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeConstCurveFloat")]
    [Display("Constant")]
    public class ComputeConstCurveFloat : ComputeConstCurve<float> { }

    /// <summary>
    /// Function float value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeFunctionCurveFloat")]
    [Display("Function")]
    public class ComputeFunctionCurveFloat : ComputeFunctionCurve<float>
    {
        protected override float GetElementFrom(float value) { return value; }
    }

    /// <summary>
    /// Binary operator float value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeBinaryCurveFloat")]
    [Display("Binary Operation")]
    public class ComputeBinaryCurveFloat : ComputeBinaryCurve<float>
    {
        /// <inheritdoc/>
        protected override float Add(float a, float b)
        {
            return a + b;
        }

        /// <inheritdoc/>
        protected override float Subtract(float a, float b)
        {
            return a - b;
        }

        /// <inheritdoc/>
        protected override float Multiply(float a, float b)
        {
            return a * b;
        }
    }

    /// <summary>
    /// Animation of a float value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeAnimationCurveFloat")]
    [Display("Animation", Expand = ExpandRule.Never)]
    public class ComputeAnimationCurveFloat : ComputeAnimationCurve<float>
    {
        /// <inheritdoc/>
        public override void Cubic(ref float value1, ref float value2, ref float value3, ref float value4, float t, out float result)
        {
            result = Interpolator.Cubic(value1, value2, value3, value4, t);
        }

        /// <inheritdoc/>
        public override void Linear(ref float value1, ref float value2, float t, out float result)
        {
            result = Interpolator.Linear(value1, value2, t);
        }
    }

    #endregion

    #region Quaternion
    /// <summary>
    /// Sampler container for Quaternion data type
    /// </summary>
    [DataContract("ComputeCurveSamplerQuaternion")]
    [Display("Sampler Quaternion")]
    public class ComputeCurveSamplerQuaternion : ComputeCurveSampler<Quaternion>
    {
        public ComputeCurveSamplerQuaternion()
        {
            curve = new ComputeAnimationCurveQuaternion();
        }

        /// <inheritdoc/>
        public override void Linear(ref Quaternion value1, ref Quaternion value2, float t, out Quaternion result)
        {
            Interpolator.Quaternion.SphericalLinear(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Constant Quaternion value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeConstCurveQuaternion")]
    [Display("Constant")]
    public class ComputeConstCurveQuaternion : ComputeConstCurve<Quaternion> { }

    /// <summary>
    /// Constant Quaternion value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeAnimationCurveQuaternion")]
    [Display("Animation", Expand = ExpandRule.Never)]
    public class ComputeAnimationCurveQuaternion : ComputeAnimationCurve<Quaternion>
    {
        /// <inheritdoc/>
        public override void Cubic(ref Quaternion value1, ref Quaternion value2, ref Quaternion value3, ref Quaternion value4, float t, out Quaternion result)
        {
            Interpolator.Quaternion.Cubic(ref value1, ref value2, ref value3, ref value4, t, out result);
        }

        /// <inheritdoc/>
        public override void Linear(ref Quaternion value1, ref Quaternion value2, float t, out Quaternion result)
        {
            Interpolator.Quaternion.SphericalLinear(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Binary operator Quaternion value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeBinaryCurveQuaternion")]
    [Display("Binary Operation")]
    public class ComputeBinaryCurveQuaternion : ComputeBinaryCurve<Quaternion>
    {
        /// <inheritdoc/>
        protected override Quaternion Add(Quaternion a, Quaternion b)
        {
            return a + b;
        }

        /// <inheritdoc/>
        protected override Quaternion Subtract(Quaternion a, Quaternion b)
        {
            return a - b;
        }

        /// <inheritdoc/>
        protected override Quaternion Multiply(Quaternion a, Quaternion b)
        {
            return a * b;
        }
    }

    #endregion

    #region Color4
    /// <summary>
    /// Sampler container for Color4 data type
    /// </summary>
    [DataContract("ComputeCurveSamplerColor4")]
    [Display("Sampler Color4")]
    public class ComputeCurveSamplerColor4 : ComputeCurveSampler<Color4>
    {
        public ComputeCurveSamplerColor4()
        {
            curve = new ComputeAnimationCurveColor4();
        }

        /// <inheritdoc/>
        public override void Linear(ref Color4 value1, ref Color4 value2, float t, out Color4 result)
        {
            Color4.Lerp(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Constant Color4 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeConstCurveColor4")]
    [Display("Constant")]
    public class ComputeConstCurveColor4 : ComputeConstCurve<Color4> { }

    /// <summary>
    /// Function Color4 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeFunctionCurveColor4")]
    [Display("Function")]
    public class ComputeFunctionCurveColor4 : ComputeFunctionCurve<Color4>
    {
        protected override Color4 GetElementFrom(float value) { return new Color4(value, value, value, value); }
    }

    /// <summary>
    /// Constant Color4 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeAnimationCurveColor4")]
    [Display("Animation", Expand = ExpandRule.Never)]
    public class ComputeAnimationCurveColor4 : ComputeAnimationCurve<Color4>
    {
        /// <inheritdoc/>
        public override void Cubic(ref Color4 value1, ref Color4 value2, ref Color4 value3, ref Color4 value4, float t, out Color4 result)
        {
            // FIXME: does it make sense to use the same kind of cubic computation than Vector4 for colors?

            Vector4 vector1 = value1;
            Vector4 vector2 = value2;
            Vector4 vector3 = value3;
            Vector4 vector4 = value4;
            Vector4 vectorR;
            Interpolator.Vector4.Cubic(ref vector1, ref vector2, ref vector3, ref vector4, t, out vectorR);
            value1 = (Color4)vector1;
            value2 = (Color4)vector2;
            value3 = (Color4)vector3;
            value4 = (Color4)vector4;
            result = (Color4)vectorR;
        }

        /// <inheritdoc/>
        public override void Linear(ref Color4 value1, ref Color4 value2, float t, out Color4 result)
        {
            Color4.Lerp(ref value1, ref value2, t, out result);
        }
    }

    /// <summary>
    /// Binary operator Color4 value for the IComputeCurve interface
    /// </summary>
    [DataContract("ComputeBinaryCurveColor4")]
    [Display("Binary Operation")]
    public class ComputeBinaryCurveColor4 : ComputeBinaryCurve<Color4>
    {
        /// <inheritdoc/>
        protected override Color4 Add(Color4 a, Color4 b)
        {
            return a + b;
        }

        /// <inheritdoc/>
        protected override Color4 Subtract(Color4 a, Color4 b)
        {
            return a - b;
        }

        /// <inheritdoc/>
        protected override Color4 Multiply(Color4 a, Color4 b)
        {
            return a * b;
        }
    }

    #endregion
}
