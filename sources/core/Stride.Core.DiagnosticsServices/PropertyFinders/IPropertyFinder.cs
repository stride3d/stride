using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrideDiagnostics.PropertyFinders;
public interface IPropertyFinder
{
    public IEnumerable<IPropertySymbol> Find(ref INamedTypeSymbol baseType);
}
