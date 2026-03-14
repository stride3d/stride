// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.Versioning;
using Stride.Core.AssemblyProcessor.Serializers;
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor;

internal class SerializerRegistry
{
    public List<TypeReference> ReferencedAssemblySerializerFactoryTypes { get; } = [];

    public CecilSerializerContext Context { get; }

    public string? TargetFramework { get; }

    public string ClassName { get; }

    public AssemblyDefinition Assembly { get; }

    public List<ICecilSerializerDependency> SerializerDependencies { get; } = [];

    public List<ICecilSerializerFactory> SerializerFactories { get; } = [];

    public SerializerRegistry(PlatformType platform, AssemblyDefinition assembly, TextWriter log)
    {
        Assembly = assembly;
        ClassName = Utilities.BuildValidClassName(assembly.Name.Name) + "SerializerFactory";

        // Register referenced assemblies serializer factory, so that we can call them recursively
        foreach (var referencedAssemblyName in assembly.MainModule.AssemblyReferences)
        {
            try
            {
                var referencedAssembly = assembly.MainModule.AssemblyResolver.Resolve(referencedAssemblyName);

                var assemblySerializerFactoryType = GetSerializerFactoryType(referencedAssembly);
                if (assemblySerializerFactoryType != null)
                    ReferencedAssemblySerializerFactoryTypes.Add(assemblySerializerFactoryType);
            }
            catch (AssemblyResolutionException)
            {
                continue;
            }
        }

        // Find target framework and replicate it for serializer assembly.
        var targetFrameworkAttribute = assembly.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName);
        if (targetFrameworkAttribute != null)
        {
            TargetFramework = "\"" + (string)targetFrameworkAttribute.ConstructorArguments[0].Value + "\"";
            var frameworkDisplayNameField = targetFrameworkAttribute.Properties.FirstOrDefault(x => x.Name == "FrameworkDisplayName");
            if (frameworkDisplayNameField.Name != null)
            {
                TargetFramework += ", FrameworkDisplayName=\"" + (string)frameworkDisplayNameField.Argument.Value + "\"";
            }
        }

        // Prepare serializer processors
        Context = new CecilSerializerContext(platform, assembly, log);
        var processors = new List<ICecilSerializerProcessor>
        {
            // Import list of serializer registered by referenced assemblies
            new ReferencedAssemblySerializerProcessor(),

            // Discover [DataContract] types and resolve their serializers
            new CecilDataContractSerializerProcessor(),

            // Generate serializers for PropertyKey and ParameterKey
            new PropertyKeySerializerProcessor(),

            // Update Engine (with AnimationData<T>)
            new UpdateEngineProcessor(),

            // Profile serializers
            new ProfileSerializerProcessor(),

            // Data contract aliases
            new DataContractAliasProcessor()
        };

        // Apply each processor
        foreach (var processor in processors)
            processor.ProcessSerializers(Context);
    }

    private static TypeDefinition? GetSerializerFactoryType(AssemblyDefinition referencedAssembly)
    {
        var assemblySerializerFactoryAttribute =
            referencedAssembly.CustomAttributes.FirstOrDefault(
                x =>
                    x.AttributeType.FullName ==
                    "Stride.Core.Serialization.AssemblySerializerFactoryAttribute");

        // No serializer factory?
        if (assemblySerializerFactoryAttribute == null)
            return null;

        var typeReference = (TypeReference)assemblySerializerFactoryAttribute.Fields.Single(x => x.Name == "Type").Argument.Value;
        if (typeReference == null)
            return null;

        return typeReference.Resolve();
    }
}
