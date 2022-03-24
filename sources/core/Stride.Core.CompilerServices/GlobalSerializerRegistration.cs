using Microsoft.CodeAnalysis;
using Stride.Core.Serialization;

namespace Stride.Core.CompilerServices
{
    internal class GlobalSerializerRegistration
    {
        public ITypeSymbol DataType { get; set; }
        public ITypeSymbol SerializerType { get; set; }
        public DataSerializerGenericMode GenericMode { get; set; }
        public bool Inherited { get; set; }
        public bool Generated { get; set; }
    }
}
