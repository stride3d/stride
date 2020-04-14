// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A Storage qualifier.
    /// </summary>
    public partial class StorageQualifier
    {
        #region Constants and Fields

        /// <summary>
        ///   Const qualifier.
        /// </summary>
        public static readonly Qualifier Const = new Qualifier("const");

        /// <summary>
        ///   Uniform qualifier.
        /// </summary>
        public static readonly Qualifier Uniform = new Qualifier("uniform");

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
            if (enumName == (string)Const.Key)
                return Const;
            if (enumName == (string)Uniform.Key)
                return Uniform;

            throw new ArgumentException(string.Format("Unable to convert [{0}] to qualifier", enumName), "key");
        }

        #endregion
    }
}
