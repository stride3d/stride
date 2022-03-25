using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Stride.Core.Serialization;

namespace Stride.Core.CompilerServices
{
    internal class GlobalSerializerRegistration
    {
        public ITypeSymbol DataType { get; set; }
        
        /// <summary>
        /// Symbol of the serializer type.
        /// </summary>
        /// <remarks>
        /// If null, serializer type is emitted via this generator so we can figure out its name.
        /// </remakrs>
        public INamedTypeSymbol SerializerType { get; set; }
        
        public DataSerializerGenericMode GenericMode { get; set; }
        
        public bool Inherited { get; set; }
        
        public bool Generated { get; set; }

        public string Profile { get; set; } = "Default";
    }
}
