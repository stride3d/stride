using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    [DebuggerDisplay("{Name} ({Type})")]
    internal class SerializerMemberSpec : IComparable<SerializerMemberSpec>
    {
        public string Name { get; }
        public ITypeSymbol Type { get; }
        public ISymbol Member { get; }
        public int? Order { get; }

        public SerializerMemberSpec(ISymbol memberSymbol, ITypeSymbol type, int? order)
        {
            Name = memberSymbol.Name;
            Type = type;
            Order = order;
        }

        public int CompareTo(SerializerMemberSpec other)
        {
            return Order == other.Order ? 0 : (Order < other.Order ? -1 : 1);
        }
    }
}
