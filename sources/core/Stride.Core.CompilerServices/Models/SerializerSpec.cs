using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices.Models
{
    internal class SerializerSpec
    {
        public IAssemblySymbol Assembly { get; set; }
        public List<INamedTypeSymbol> AllTypes { get; set; }
        public HashSet<SerializerTypeSpec> DataContractTypes { get; set; }
        public HashSet<ITypeSymbol> InheritedCustomSerializableTypes { get; set; }
        public ProfiledDictionary<ITypeSymbol, GlobalSerializerRegistration> DependencySerializerReference { get; set; }
        public ProfiledDictionary<ITypeSymbol, GlobalSerializerRegistration> GlobalSerializerRegistrationsToEmit { get; set; }
    }
}
