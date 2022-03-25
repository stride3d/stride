using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    [DebuggerDisplay("Type={Type}, Members: {Members.Count}")]
    internal class SerializerTypeSpec
    {
        private readonly string typeName;
        public INamedTypeSymbol Type { get; private set; }

        /// <summary>
        /// Base type of Type, or null if base type is System.Object or the type is not serializable.
        /// </summary>
        public INamedTypeSymbol BaseType { get; set; }

        public List<SerializerMemberSpec> Members { get; private set; }

        /// <summary>
        /// Was DataContract inherited from base class?
        /// </summary>
        public bool Inherited { get; set; }

        public List<string> Aliases { get; } = new List<string>();

        public SerializerTypeSpec(INamedTypeSymbol type, List<SerializerMemberSpec> members)
        {
            Type = type;
            Members = members;
            typeName = type.ToDisplayString();
        }

        public class EqualityComparer : IEqualityComparer<SerializerTypeSpec>
        {
            public bool Equals(SerializerTypeSpec x, SerializerTypeSpec y)
            {
                return x.typeName == y.typeName;
            }

            public int GetHashCode(SerializerTypeSpec obj)
            {
                return obj.typeName.GetHashCode();
            }
        }
    }
}
