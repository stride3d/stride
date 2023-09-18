using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

public interface IPropertyFinder
{
    public IEnumerable<IPropertySymbol> Find(ref INamedTypeSymbol baseType);
}
