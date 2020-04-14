// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;

namespace Stride.Core.Shaders.Convertor
{

    internal static class AstExtensions
    {
        public static Semantic Semantic(this Variable declarator)
        {
            return declarator.Qualifiers.OfType<Semantic>().LastOrDefault();
        }

        public static Semantic Semantic(this MethodDeclaration methodDeclaration)
        {
            return methodDeclaration.Qualifiers.OfType<Semantic>().LastOrDefault();
        }

        public static IEnumerable<AttributeDeclaration> Attributes(this MethodDeclaration methodDeclaration)
        {
            return methodDeclaration.Attributes.OfType<AttributeDeclaration>();
        }

        public static IEnumerable<Variable> Fields(this StructType structType)
        {
            return structType.Fields.SelectMany(variable => variable.Instances());
        }
    }
}
