// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A Storage qualifier.
    /// </summary>
    public static class StorageQualifier
    {
        #region Constants and Fields

        /// <summary>
        ///   ColumnMajor modifier.
        /// </summary>
        public static readonly Qualifier ColumnMajor = new Qualifier("column_major");

        /// <summary>
        ///   Extern modifier.
        /// </summary>
        public static readonly Qualifier Extern = new Qualifier("extern");

        /// <summary>
        ///   Groupshared modifier.
        /// </summary>
        public static readonly Qualifier Groupshared = new Qualifier("groupshared");

        /// <summary>
        ///   Precise modifier.
        /// </summary>
        public static readonly Qualifier Precise = new Qualifier("precise");

        /// <summary>
        ///   RowMajor modifier.
        /// </summary>
        public static readonly Qualifier RowMajor = new Qualifier("row_major");

        /// <summary>
        ///   Shared modifier.
        /// </summary>
        public static readonly Qualifier Shared = new Qualifier("shared");

        /// <summary>
        ///   Static modifier.
        /// </summary>
        public static readonly Qualifier Static = new Qualifier("static");

        /// <summary>
        ///   Inline modifier.
        /// </summary>
        public static readonly Qualifier Inline = new Qualifier("inline");

        /// <summary>
        ///   Unsigned modifier.
        /// </summary>
        public static readonly Qualifier Unsigned = new Qualifier("unsigned");

        /// <summary>
        ///   Volatile modifier.
        /// </summary>
        public static readonly Qualifier Volatile = new Qualifier("volatile");

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
            if (enumName == (string)ColumnMajor.Key)
                return ColumnMajor;
            if (enumName == (string)Extern.Key)
                return Extern;
            if (enumName == (string)Groupshared.Key)
                return Groupshared;
            if (enumName == (string)Precise.Key)
                return Precise;
            if (enumName == (string)Precise.Key)
                return Precise;
            if (enumName == (string)RowMajor.Key)
                return RowMajor;
            if (enumName == (string)Shared.Key)
                return Shared;
            if (enumName == (string)Static.Key)
                return Static;
            if (enumName == (string)Inline.Key)
                return Inline;
            if (enumName == (string)Unsigned.Key)
                return Unsigned;
            if (enumName == (string)Volatile.Key)
                return Volatile;

            // Fallback to parameter interpolation qualifiers
            var result = InterpolationQualifier.Parse(enumName);
            if (result != null)
                return result;

            // Fallback to shared parameter qualifiers
            return Ast.StorageQualifier.Parse(enumName);
        }

        #endregion
    }
}
