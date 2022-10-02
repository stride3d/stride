using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Stride.Core.Serialization;

namespace Stride.Core.CompilerServices.Models
{
    internal class GlobalSerializerRegistration
    {
        public ITypeSymbol DataType { get; set; }

        /// <summary>
        /// Symbol of the serializer type.
        /// </summary>
        /// <remarks>
        /// If null and <see cref="Generated"/> = true, serializer type is emitted via this generator so we can figure out its name in the emitter.
        /// </remarks>
        public INamedTypeSymbol SerializerType { get; set; }

        public DataSerializerGenericMode GenericMode { get; set; }

        public bool Inherited { get; set; }

        public bool Generated { get; set; }

        public string Profile { get; set; } = "Default";
    }
}
