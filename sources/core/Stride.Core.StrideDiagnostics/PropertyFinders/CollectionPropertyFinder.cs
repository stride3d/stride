using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

internal class CollectionPropertyFinder : IPropertyFinder, IViolationReporter
{
    public IEnumerable<IPropertySymbol> Find(ref INamedTypeSymbol baseType)
    {
        if (baseType == null)
            return Enumerable.Empty<IPropertySymbol>();
        return baseType.GetMembers().OfType<IPropertySymbol>().Where(property => !PropertyHelper.IsArray(property) && !this.ShouldBeIgnored(property) && PropertyHelper.ImplementsICollectionT(property.Type) && HasProperAccess(property));
    }

    public void ReportViolations(ref INamedTypeSymbol baseType, ClassInfo classInfo)
    {
        if (baseType == null)
            return;
        var violations = baseType.GetMembers().OfType<IPropertySymbol>().Where(property => !PropertyHelper.IsArray(property) && !this.ShouldBeIgnored(property) && PropertyHelper.ImplementsICollectionT(property.Type) && !HasProperAccess(property));
        foreach (var violation in violations)
        {

            Report(violation, classInfo);


        }
    }




    private static void Report(IPropertySymbol property, ClassInfo classInfo)
    {
        var error = new DiagnosticDescriptor(
            id: ErrorCodes.CollectionAccess,
            title: "Invalid Access",
            category: NexGenerator.CompilerServicesDiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            messageFormat: $"The Property '{property.Name}' has an invalid Access Type for an {property.Type}, expected for a ICollection<T> is a public/internal get; Accessor, Stride will not be able to use this Property as [DataMember]. Add [DataMemberIgnore] to let Stride Ignore the Member in the [DataContract] or change the get; accesibility.",
            helpLinkUri: "https://www.stride3d.net"
        );
        var location = Location.Create(classInfo.TypeSyntax.SyntaxTree, property.DeclaringSyntaxReferences.FirstOrDefault().Span);
        classInfo.ExecutionContext.ReportDiagnostic(Diagnostic.Create(error, location));
    }
    private bool HasProperAccess(IPropertySymbol property)
    {
        return property.GetMethod?.DeclaredAccessibility == Accessibility.Public ||
                property.GetMethod?.DeclaredAccessibility == Accessibility.Internal;
    }
}
