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
        public Dictionary<(ITypeSymbol, string profile), GlobalSerializerRegistration> DependencySerializerReference { get; set; }
        public Dictionary<(ITypeSymbol, string profile), GlobalSerializerRegistration> GlobalSerializerRegistrationsToEmit { get; set; }
    }
}
