using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Extensions;
using Stride.Core.Serialization;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.Serialization
{
    public static class SerializerGeneratorCompilationUtils
    {
        public static INamedTypeSymbol GetSerializationFactory(this Compilation compilation, IAssemblySymbol assembly = null)
        {
            assembly ??= compilation.Assembly;
            var assemblySerializerFactoryAttribute = compilation.GetTypeByMetadataName("Stride.Core.Serialization.AssemblySerializerFactoryAttribute");
            var factoryAttribute = assembly.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Is(assemblySerializerFactoryAttribute));
            Assert.True(factoryAttribute != null, $"No instance of AssemblySerializerFactoryAttribute has been found on assembly '{assembly.Name}'");
            Assert.True(factoryAttribute.NamedArguments.Length == 1, $"AssemblySerializerFactoryAttribute on assembly '{assembly.Name}' does not have Type field set.");
            return factoryAttribute.NamedArguments[0].Value.Value as INamedTypeSymbol;
        }

        public static AttributeData[] GetAttributes(this ITypeSymbol type, Compilation compilation, string attributeFullName)
        {
            var attribute = compilation.GetTypeByMetadataName(attributeFullName);
            return type.GetAttributes().Where(attr => attr.AttributeClass.Is(attribute)).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertDataSerializerGlobal(this AttributeData attribute, INamedTypeSymbol serializerType, ITypeSymbol dataType, DataSerializerGenericMode mode, bool inherited, bool generated, string profile = "Default")
        {
            var attributeSerializerType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
            Assert.True(
                (attributeSerializerType == null && serializerType == null) ||
                attributeSerializerType.Is(serializerType),
                $"SerializerType mismatch. Expected '{serializerType?.ToDisplayString() ?? "null"}', Actual '{attributeSerializerType?.ToDisplayString() ?? "null"}'");

            var attributeDataType = attribute.ConstructorArguments[1].Value as ITypeSymbol;
            Assert.True(
                (attributeDataType == null && dataType == null) ||
                attributeDataType.Is(dataType),
                $"DataType mismatch. Expected '{dataType?.ToDisplayString() ?? "null"}', Actual '{attributeDataType?.ToDisplayString() ?? "null"}'");

            var attributeMode = (DataSerializerGenericMode)(int)attribute.ConstructorArguments[2].Value;
            Assert.True(attributeMode == mode,
                $"GenericMode mismatch. Expected '{mode}', Actual '{attributeMode}'");

            var attributeInherited = (bool)attribute.ConstructorArguments[3].Value;
            Assert.True(attributeInherited == inherited,
                $"Inherited mismatch. Expected '{inherited}', Actual '{attributeInherited}'");

            var attributeGenerated = (bool)attribute.ConstructorArguments[4].Value;
            Assert.True(attributeGenerated == generated,
                $"Inherited mismatch. Expected '{generated}', Actual '{attributeGenerated}'");

            var attributeProfile = attribute.NamedArguments.Length > 0
                ? (string)attribute.NamedArguments[0].Value.Value
                : "Default";
            Assert.True(attributeProfile == profile,
                $"Inherited mismatch. Expected '{profile}', Actual '{attributeProfile}'");
        }
    }
}
