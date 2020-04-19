// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// Value range for a float
    /// </summary>
    public partial class FloatQualifier
    {
        #region Constants and Fields

        /// <summary>
        ///   IEEE 32-bit signed-normalized float in range -1 to 1 inclusive.
        /// </summary>
        public static readonly Qualifier SNorm = new Qualifier("snorm");

        /// <summary>
        ///   IEEE 32-bit unsigned-normalized float in range 0 to 1 inclusive.
        /// </summary>
        public static readonly Qualifier UNorm = new Qualifier("unorm");

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses the specified enum name.
        /// </summary>
        /// <param name="enumName">
        /// Name of the enum.
        /// </param>
        /// <returns>
        /// A qualifier
        /// </returns>
        public static Qualifier Parse(string enumName)
        {
            if (enumName == (string)SNorm.Key)
                return SNorm;
            if (enumName == (string)UNorm.Key)
                return UNorm;

            throw new ArgumentException(string.Format("Unable to convert [{0}] to qualifier", enumName), "key");
        }

        #endregion
    }
}
