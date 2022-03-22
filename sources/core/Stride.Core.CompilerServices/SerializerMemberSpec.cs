using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    [DebuggerDisplay("{Name} ({Type})")]
    internal class SerializerMemberSpec
    {
        public string Name { get; }
        public ITypeSymbol Type { get; }

        public SerializerMemberSpec(string memberName, ITypeSymbol type)
        {
            Name = memberName;
            Type = type;
        }
    }
}
