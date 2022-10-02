using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    internal class WellKnownReferences
    {
        public WellKnownReferences(Compilation compilation)
        {
            SystemObject = compilation.GetSpecialType(SpecialType.System_Object);
            SystemInt32 = compilation.GetSpecialType(SpecialType.System_Int32);

            AssemblySerializerFactoryAttribute = compilation.GetTypeByMetadataName("Stride.Core.Serialization.AssemblySerializerFactoryAttribute");
            EnumSerializer = compilation.GetTypeByMetadataName("Stride.Core.Serialization.Serializers.EnumSerializer`1");
            DataAliasAttribute = compilation.GetTypeByMetadataName("Stride.Core.DataAliasAttribute");
            DataContractAttribute = compilation.GetTypeByMetadataName("Stride.Core.DataContractAttribute");
            DataMemberAttribute = compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");
            DataMemberIgnoreAttribute = compilation.GetTypeByMetadataName("Stride.Core.DataMemberIgnoreAttribute");
            DataSerializer = compilation.GetTypeByMetadataName("Stride.Core.Serialization.DataSerializer`1");
            DataSerializerAttribute = compilation.GetTypeByMetadataName("Stride.Core.Serialization.DataSerializerAttribute");
            DataSerializerGlobalAttribute = compilation.GetTypeByMetadataName("Stride.Core.Serialization.DataSerializerGlobalAttribute");

            InteropFieldOffsetAttribute = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.FieldOffsetAttribute");
            InteropStructLayoutAttribute = compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
        }

        public INamedTypeSymbol SystemInt32 { get; }
        public INamedTypeSymbol SystemObject { get; }

        public INamedTypeSymbol AssemblySerializerFactoryAttribute { get; }
        public INamedTypeSymbol EnumSerializer { get; }
        public INamedTypeSymbol DataAliasAttribute { get; }
        public INamedTypeSymbol DataContractAttribute { get; }
        public INamedTypeSymbol DataMemberAttribute { get; }
        public INamedTypeSymbol DataMemberIgnoreAttribute { get; }
        public INamedTypeSymbol DataSerializer { get; }
        public INamedTypeSymbol DataSerializerAttribute { get; }
        public INamedTypeSymbol DataSerializerGlobalAttribute { get; }

        public INamedTypeSymbol InteropFieldOffsetAttribute { get; }
        public INamedTypeSymbol InteropStructLayoutAttribute { get; }
    }
}
