using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    [Flags]
    internal enum MemberAccessMode
    {
        /// <summary>
        /// Access member directly by reference (most fields).
        /// </summary>
        ByRef = 1,
        /// <summary>
        /// Access member by doing a local copy and referencing that (properties and readonly fields).
        /// </summary>
        ByLocalRef = 2,
        /// <summary>
        /// Assign back from local when deserializing (properties).
        /// </summary>
        WithAssignment = 4,
    }

    [DebuggerDisplay("{Name} ({Type})")]
    internal class SerializerMemberSpec : IComparable<SerializerMemberSpec>
    {
        public string Name { get; }
        public ITypeSymbol Type { get; }
        public ISymbol Member { get; }
        public int? Order { get; }
        /// <summary>
        /// Can you make <c>ref</c> to this member (when it's a field) or do you need to make a local.
        /// </summary>
        public MemberAccessMode AccessMode { get; }

        public SerializerMemberSpec(ISymbol memberSymbol, ITypeSymbol type, int? order, MemberAccessMode accessMode)
        {
            Member = memberSymbol;
            Name = memberSymbol.Name;
            Type = type;
            Order = order;
            AccessMode = accessMode;
        }

        public int CompareTo(SerializerMemberSpec other)
        {
            return Order == other.Order ? 0 : (Order < other.Order ? -1 : 1);
        }
    }
}
