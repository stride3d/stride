// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    class FixupValueTypeVisitor : CecilTypeReferenceVisitor
    {
        public static readonly FixupValueTypeVisitor Default = new FixupValueTypeVisitor();

        public override TypeReference Visit(TypeReference type)
        {
            var typeDefinition = type.Resolve();
            if (typeDefinition != null && typeDefinition.IsValueType && !type.IsValueType)
                type.IsValueType = typeDefinition.IsValueType;

            return base.Visit(type);
        }

        public override TypeReference Visit(GenericInstanceType type)
        {
            type = (GenericInstanceType)base.Visit(type);
            if (type.ElementType.IsValueType && !type.IsValueType)
                type.IsValueType = type.ElementType.IsValueType;

            return type;
        }
    }
}
