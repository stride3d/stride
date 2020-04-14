// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Mono.Cecil.Rocks;
using Xenko.Core.AssemblyProcessor.Serializers;

namespace Xenko.Core.AssemblyProcessor
{
    /// <summary>
    /// Collects DataContractAttribute.Alias so that they are registered during Module initialization.
    /// </summary>
    internal class DataContractAliasProcessor : ICecilSerializerProcessor
    {
        public void ProcessSerializers(CecilSerializerContext context)
        {
            foreach (var type in context.Assembly.MainModule.GetAllTypes())
            {
                foreach (var dataContractAttribute in type.CustomAttributes.Where(x => x.AttributeType.FullName == "Xenko.Core.DataContractAttribute" || x.AttributeType.FullName == "Xenko.Core.DataAliasAttribute"))
                {
                    // Only process if ctor with 1 argument
                    if (!dataContractAttribute.HasConstructorArguments || dataContractAttribute.ConstructorArguments.Count != 1)
                        continue;

                    var alias = (string)dataContractAttribute.ConstructorArguments[0].Value;

                    // Third parameter is IsAlias (differentiate DataAlias from DataContract)
                    context.DataContractAliases.Add(Tuple.Create(alias, type, dataContractAttribute.AttributeType.FullName == "Xenko.Core.DataAliasAttribute"));
                }
            }
        }
    }
}
