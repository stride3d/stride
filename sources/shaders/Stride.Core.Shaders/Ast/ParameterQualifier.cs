// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A Storage qualifier.
    /// </summary>
    public static class ParameterQualifier
    {
        #region Constants and Fields

        /// <summary>
        ///   In modifier, only for method parameters.
        /// </summary>
        public static readonly Qualifier In = new Qualifier("in");

        /// <summary>
        ///   InOut Modifier, only for method parameters.
        /// </summary>
        public static readonly Qualifier InOut = new Qualifier("inout");

        /// <summary>
        ///   Out modifier, only for method parameters.
        /// </summary>
        public static readonly Qualifier Out = new Qualifier("out");

        /// <summary>
        ///   Flat modifier, only for inputs or outputs.
        /// </summary>
        public static readonly Qualifier Flat = new Qualifier("flat");

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses the specified enum name.
        /// </summary>
        /// <param name="enumName">
        /// Name of the enum.
        /// </param>
        /// <returns>
        /// A parameter qualifier
        /// </returns>
        public static Qualifier Parse(string enumName)
        {
            if (enumName == (string)In.Key)
                return In;
            if (enumName == (string)InOut.Key)
                return InOut;
            if (enumName == (string)Out.Key)
                return Out;
            if (enumName == (string)Flat.Key)
                return Flat;

            throw new ArgumentException(string.Format("Unable to convert [{0}] to qualifier", enumName), "key");
        }

        #endregion
    }
}
