// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Convertor
{
    public static class HlslTypes
    {
        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A Typedeclaration and dimensions</returns>
        public static Tuple<TypeBase, int, int> GetType(string type)
        {
            string prefix = null;
            if (type.StartsWith("matrix", StringComparison.Ordinal))
            {
                var dimStr = type["matrix".Length..];
                if (dimStr.Length == 0)
                {
                    return new Tuple<TypeBase, int, int>(new MatrixType(), 4, 4);
                }

                return new Tuple<TypeBase, int, int>(new MatrixType(), int.Parse(dimStr[0].ToString()), int.Parse(dimStr[2].ToString()));
            }

            TypeBase declaration = null;

            if (type.StartsWith("float", StringComparison.Ordinal))
            {
                prefix = "float";
                declaration = ScalarType.Float;
            }
            else if (type.StartsWith("int", StringComparison.Ordinal))
            {
                prefix = "int";
                declaration = ScalarType.Int;
            }
            else if (type.StartsWith("half", StringComparison.Ordinal))
            {
                prefix = "half";
                declaration = ScalarType.Half;
            }
            else if (type.StartsWith("uint", StringComparison.Ordinal))
            {
                prefix = "uint";
                declaration = ScalarType.UInt;
            }
            else if (type.StartsWith("bool", StringComparison.Ordinal))
            {
                prefix = "bool";
                declaration = ScalarType.Bool;
            }
            else if (type.StartsWith("double", StringComparison.Ordinal))
            {
                prefix = "double";
                declaration = ScalarType.Double;
            }

            if (prefix == null)
            {
                return null;
            }

            return new Tuple<TypeBase, int, int>(declaration, int.Parse(type.Substring(prefix.Length, 1)), 0);
        }
   }
}
