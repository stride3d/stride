using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    internal class SerializerSpec
    {
        public IAssemblySymbol Assembly { get; set; }
        public HashSet<SerializerTypeSpec> DataContractTypes { get; set; }
        public Dictionary<ITypeSymbol, GlobalSerializerRegistration> DependencySerializerReference { get; set; }
        public Dictionary<ITypeSymbol, GlobalSerializerRegistration> GlobalSerializerRegistrationsToEmit { get; set; }
    }
}
