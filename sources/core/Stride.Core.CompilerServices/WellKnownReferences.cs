using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    internal class WellKnownReferences
    {
        public WellKnownReferences(Compilation compilation)
        {
            SystemObject = compilation.GetSpecialType(SpecialType.System_Object);
            SystemInt32 = compilation.GetSpecialType(SpecialType.System_Int32);

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

        public INamedTypeSymbol SystemInt32 { get; private set; }
        public INamedTypeSymbol SystemObject { get; private set; }

        public INamedTypeSymbol EnumSerializer { get; private set; }
        public INamedTypeSymbol DataAliasAttribute { get; private set; }
        public INamedTypeSymbol DataContractAttribute { get; private set; }
        public INamedTypeSymbol DataMemberAttribute { get; private set; }
        public INamedTypeSymbol DataMemberIgnoreAttribute { get; private set; }
        public INamedTypeSymbol DataSerializer { get; private set; }
        public INamedTypeSymbol DataSerializerAttribute { get; private set; }
        public INamedTypeSymbol DataSerializerGlobalAttribute { get; private set; }

        public INamedTypeSymbol InteropFieldOffsetAttribute { get; private set; }
        public INamedTypeSymbol InteropStructLayoutAttribute { get; private set; }
    }
}
