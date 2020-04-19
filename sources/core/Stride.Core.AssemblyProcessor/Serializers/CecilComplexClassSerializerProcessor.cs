// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Stride.Core.AssemblyProcessor.Serializers
{
    class CecilComplexClassSerializerProcessor : ICecilSerializerProcessor
    {
        public void ProcessSerializers(CecilSerializerContext context)
        {
            foreach (var type in context.Assembly.MainModule.GetAllTypes().ToArray())
            {
                // Force generation of serializers (complex types, etc...)
                // Check complex type definitions
                ProcessType(context, type);
            }
        }

        private static void ProcessType(CecilSerializerContext context, TypeDefinition type)
        {
            CecilSerializerContext.SerializableTypeInfo serializableTypeInfo;
            if (!context.SerializableTypes.TryGetSerializableTypeInfo(type, false, out serializableTypeInfo)
                && !context.SerializableTypes.TryGetSerializableTypeInfo(type, true, out serializableTypeInfo))
            {
                context.FindSerializerInfo(type, false);
            }

            if (type.HasNestedTypes)
            {
                foreach (var nestedType in type.NestedTypes)
                {
                    ProcessType(context, nestedType);
                }
            }
        }
    }
}
