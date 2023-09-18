using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.Core.StrideDiagnostics;
internal class WellKnownReferences
{
    public WellKnownReferences(Compilation compilation)
    {
        DataContractAttribute = compilation.GetTypeByMetadataName("Stride.Core.DataContractAttribute");
        DataMemberAttribute = compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");
        DataMemberIgnoreAttribute = compilation.GetTypeByMetadataName("Stride.Core.DataMemberIgnoreAttribute");
    }


    public INamedTypeSymbol DataContractAttribute { get; }
    public INamedTypeSymbol DataMemberAttribute { get; }
    public INamedTypeSymbol DataMemberIgnoreAttribute { get; }
}