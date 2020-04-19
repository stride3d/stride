// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// The U2Filmic operator.
    /// </summary>
    /// <remarks>
    /// http://filmicgames.com/archives/75
    /// </remarks>
    [DataContract("ToneMapU2FilmicOperator")]
    [Display("U2-Filmic")]
    public class ToneMapU2FilmicOperator : ToneMapOperator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToneMapU2FilmicOperator"/> class.
        /// </summary>
        public ToneMapU2FilmicOperator()
            : base("ToneMapU2FilmicOperatorShader")
        {
        }

        /// <summary>
        /// Gets or sets the shoulder strength.
        /// </summary>
        /// <value>The shoulder strength.</value>
        [DataMember(10)]
        [DefaultValue(0.22f)]
        public float ShoulderStrength
        {
            get
            {
                return Parameters.Get(ToneMapU2FilmicOperatorShaderKeys.ShoulderStrength);
            }
            set
            {
                Parameters.Set(ToneMapU2FilmicOperatorShaderKeys.ShoulderStrength, value);
            }
        }

        /// <summary>
        /// Gets or sets the linear strength.
        /// </summary>
        /// <value>The linear strength.</value>
        [DataMember(20)]
        [DefaultValue(0.25f)]
        public float LinearStrength
        {
            get
            {
                return Parameters.Get(ToneMapU2FilmicOperatorShaderKeys.LinearStrength);
            }
            set
            {
                Parameters.Set(ToneMapU2FilmicOperatorShaderKeys.LinearStrength, value);
            }
        }

        /// <summary>
        /// Gets or sets the linear angle.
        /// </summary>
        /// <value>The linear angle.</value>
        [DataMember(30)]
        [DefaultValue(0.1f)]
        public float LinearAngle
        {
            get
            {
                return Parameters.Get(ToneMapU2FilmicOperatorShaderKeys.LinearAngle);
            }
            set
            {
                Parameters.Set(ToneMapU2FilmicOperatorShaderKeys.LinearAngle, value);
            }
        }

        /// <summary>
        /// Gets or sets the toe strength.
        /// </summary>
        /// <value>The toe strength.</value>
        [DataMember(40)]
        [DefaultValue(0.2f)]
        public float ToeStrength
        {
            get
            {
                return Parameters.Get(ToneMapU2FilmicOperatorShaderKeys.ToeStrength);
            }
            set
            {
                Parameters.Set(ToneMapU2FilmicOperatorShaderKeys.ToeStrength, value);
            }
        }

        /// <summary>
        /// Gets or sets the toe numerator.
        /// </summary>
        /// <value>The toe numerator.</value>
        [DataMember(50)]
        [DefaultValue(0.01f)]
        public float ToeNumerator
        {
            get
            {
                return Parameters.Get(ToneMapU2FilmicOperatorShaderKeys.ToeNumerator);
            }
            set
            {
                Parameters.Set(ToneMapU2FilmicOperatorShaderKeys.ToeNumerator, value);
            }
        }

        /// <summary>
        /// Gets or sets the toe denominator.
        /// </summary>
        /// <value>The toe denominator.</value>
        [DataMember(60)]
        [DefaultValue(0.3f)]
        public float ToeDenominator
        {
            get
            {
                return Parameters.Get(ToneMapU2FilmicOperatorShaderKeys.ToeDenominator);
            }
            set
            {
                Parameters.Set(ToneMapU2FilmicOperatorShaderKeys.ToeDenominator, value);
            }
        }

        /// <summary>
        /// Gets or sets the linear white.
        /// </summary>
        /// <value>The linear white.</value>
        [DataMember(70)]
        [DefaultValue(11.2f)]
        public float LinearWhite
        {
            get
            {
                return Parameters.Get(ToneMapU2FilmicOperatorShaderKeys.LinearWhite);
            }
            set
            {
                Parameters.Set(ToneMapU2FilmicOperatorShaderKeys.LinearWhite, value);
            }
        }
    }
}
