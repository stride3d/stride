using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Stride.Core.Serialization;

namespace Stride.Core.CompilerServices
{
    public partial class SerializerGenerator
    {
        private const string AssemblyProcessedAttribute = "Stride.Core.AssemblyProcessedAttribute";
        internal class Validator
        {
            private Dictionary<ITypeSymbol, GlobalSerializerRegistration> serializableTypes = new Dictionary<ITypeSymbol, GlobalSerializerRegistration>();

            public void Validate(GeneratorExecutionContext context, ref SerializerSpec serializerSpec)
            {
                CollectSerializableTypes(context);
            }

            /// <summary>
            /// Look through attributes on static types to find serializer factories and collect data from [DataSerializerGlobal]
            /// </summary>
            private static void CollectSerializableTypes(GeneratorExecutionContext context)
            {
                var assemblies = context.Compilation.References
                    .Select(context.Compilation.GetAssemblyOrModuleSymbol)
                    .OfType<IAssemblySymbol>()
                    .Cast<IAssemblySymbol>();

                foreach (var assembly in assemblies)
                {
                    // We only care about assemblies that have been processed by Stride's processor
                    if (assembly.GetTypeByMetadataName(AssemblyProcessedAttribute) == null)
                    {
                        continue;
                    }

                    CollectSerializableTypesFromNamespaceRecursive(assembly.GlobalNamespace);
                }
            }

            private static void CollectSerializableTypesFromNamespaceRecursive(INamespaceSymbol @namespace)
            {
                // TODO: figure out why the hell this doesn't return classes generated by AssemblyProcessor
                foreach (var type in @namespace.GetTypeMembers())
                {
                    if (!type.IsStatic)
                    {
                        continue;
                    }

                    foreach (var attribute in type.GetAttributes())
                    {
                        if (attribute.AttributeClass.ToDisplayString() != DataSerializerGlobalAttributeName)
                        {
                            continue;
                        }

                        if (attribute.ConstructorArguments.Length != 5 || attribute.NamedArguments.Length != 1)
                        {
                            // not all of the data has been provided - this might not be on the factory.
                            continue;
                        }

                        var serializerType = attribute.ConstructorArguments[0].Value;
                        var dataType = attribute.ConstructorArguments[1].Value;
                        var genericMode = (DataSerializerGenericMode)(int)attribute.ConstructorArguments[2].Value;
                        var inherited = (bool)attribute.ConstructorArguments[3].Value;
                        var generated = (bool)attribute.ConstructorArguments[4].Value;
                        var profile = (string)attribute.NamedArguments[0].Value.Value;

                        // TODO: build GlobalSerializerRegistration and populate dictionary
                    }
                }

                foreach (var namespaceSymbol in @namespace.GetNamespaceMembers())
                {
                    CollectSerializableTypesFromNamespaceRecursive(namespaceSymbol);
                }
            }
        }
    }
}