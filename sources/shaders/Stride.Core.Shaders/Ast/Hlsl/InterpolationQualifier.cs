// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A parameter interpolation qualifier.
    /// </summary>
    /// <seealso cref="Stride.Core.Shaders.Ast.Qualifier" />
    public class InterpolationQualifier : Qualifier
    {
        #region Constants and Fields

        /// <summary>
        ///   Centroid modifier, only valid for structure field.
        /// </summary>
        public static readonly InterpolationQualifier Centroid = new InterpolationQualifier("centroid");

        /// <summary>
        ///   Linear modifier, only valid for structure field.
        /// </summary>
        public static readonly InterpolationQualifier Linear = new InterpolationQualifier("linear");

        /// <summary>
        ///   NoPerspective modifier, only valid for structure field.
        /// </summary>
        public static readonly InterpolationQualifier NoPerspective = new InterpolationQualifier("noperspective");

        /// <summary>
        ///   Nointerpolation modifier.
        /// </summary>
        public static readonly InterpolationQualifier Nointerpolation = new InterpolationQualifier("nointerpolation");

        /// <summary>
        ///   Sample modifier, only valid for structure field.
        /// </summary>
        public static readonly InterpolationQualifier Sample = new InterpolationQualifier("sample");

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref="InterpolationQualifier" /> class.
        /// </summary>
        public InterpolationQualifier()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterpolationQualifier"/> class.
        /// </summary>
        /// <param name="key">Name of the enum.</param>
        public InterpolationQualifier(object key)
            : base(key)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses the specified enum name.
        /// </summary>
        /// <param name="enumName">
        /// Name of the enum.
        /// </param>
        /// <returns>
        /// A storage qualifier
        /// </returns>
        public static Qualifier Parse(string enumName)
        {
            if (enumName == (string)Centroid.Key)
                return Centroid;
            if (enumName == (string)Linear.Key)
                return Linear;
            if (enumName == (string)NoPerspective.Key)
                return NoPerspective;
            if (enumName == (string)Nointerpolation.Key)
                return Nointerpolation;
            if (enumName == (string)Sample.Key)
                return Sample;
            return null;
        }

        #endregion
    }
}
