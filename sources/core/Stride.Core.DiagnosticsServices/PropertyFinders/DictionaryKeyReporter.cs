using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrideDiagnostics.PropertyFinders;
internal class DictionaryKeyReporter : IViolationReporter, IPropertyFinder
{
    /// <summary>
    /// Is Always Empty, use <see cref="CollectionPropertyFinder"/> and check against <see cref="PropertyHelper.IsDictionary(IPropertySymbol)"/>
    /// </summary>
    /// <param name="baseType"></param>
    /// <returns></returns>
    public IEnumerable<IPropertySymbol> Find(ref INamedTypeSymbol baseType)
    {
        return Enumerable.Empty<IPropertySymbol>(); ;
    }

    public void ReportViolations(ref INamedTypeSymbol baseType, ClassInfo classInfo)
    {
        if (baseType == null)
            return;
        IEnumerable<IPropertySymbol> violations = baseType.GetMembers().OfType<IPropertySymbol>().Where(property => PropertyHelper.IsDictionary(property, classInfo) && !this.ShouldBeIgnored(property) && HasProperAccess(property) && InvalidDictionaryKey(property, classInfo));
        foreach (IPropertySymbol violation in violations)
        {
            Report(violation, classInfo);
        }
    }

    private bool HasProperAccess(IPropertySymbol property)
    {
        return property.GetMethod?.DeclaredAccessibility == Accessibility.Public ||
                property.GetMethod?.DeclaredAccessibility == Accessibility.Internal;
    }

    private bool InvalidDictionaryKey(IPropertySymbol property, ClassInfo info)
    {
        if (PropertyHelper.IsDictionary(property, info))
        {
            ITypeSymbol firstTypeArgument = ((INamedTypeSymbol)property.Type).TypeArguments[0];
            if (firstTypeArgument != null && !firstTypeArgument.IsValueType)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }
    private void Report(IPropertySymbol property, ClassInfo classInfo)
    {
        DiagnosticDescriptor error = new DiagnosticDescriptor(
    id: ErrorCodes.DictionaryKey,
    title: "Invalid Dictionary Key",
    category: StrideDiagnosticsGenerator.CompilerServicesDiagnosticCategory,
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    messageFormat: $"The Generic Key for '{property.Name}' is invalid, expected for a IDictionary<T,Y> is a struct/simple type Key to use this Property as [DataMember]. Add [DataMemberIgnore] to let Stride Ignore the Member in the [DataContract] or change the Dictionary Key.",
    helpLinkUri: "https://www.stride3d.net"
);
        Location location = Location.Create(classInfo.TypeSyntax.SyntaxTree, property.DeclaringSyntaxReferences.FirstOrDefault().Span);
        classInfo.ExecutionContext.ReportDiagnostic(Diagnostic.Create(error, location));

    }
}
