using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace StrideDiagnostics.PropertyFinders;
public class PropertyFinder : IPropertyFinder, IViolationReporter
{
    public IEnumerable<IPropertySymbol> Find(ref INamedTypeSymbol baseType)
    {
        if (baseType == null)
            return Enumerable.Empty<IPropertySymbol>();
        return baseType.GetMembers().OfType<IPropertySymbol>().Where(property => !PropertyHelper.IsArray(property) && !PropertyHelper.ImplementsICollectionT(property.Type) && !this.ShouldBeIgnored(property) && HasProperAccess(property));
    }

    public void ReportViolations(ref INamedTypeSymbol baseType, ClassInfo classInfo)
    {
        if (baseType == null)
            return;
        IEnumerable<IPropertySymbol> violations = baseType.GetMembers().OfType<IPropertySymbol>().Where(property => !PropertyHelper.IsArray(property) && !PropertyHelper.ImplementsICollectionT(property.Type) && !this.ShouldBeIgnored(property) && !HasProperAccess(property));
        foreach (IPropertySymbol violation in violations)
        {
            Report(violation, classInfo);
        }
    }
    private static void Report(IPropertySymbol property, ClassInfo classInfo)
    {
        DiagnosticDescriptor error = new DiagnosticDescriptor(
            id: ErrorCodes.PropertyAccess,
            title: "Invalid Access",
            category: StrideDiagnosticsGenerator.CompilerServicesDiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            messageFormat: $"The Property '{property.Name}' has an invalid Access Type for a Property, expected for non Collection/Array is a public/internal get; and a public/internal set;/init; Accessor, Stride will not be able to use this Property as [DataMember]. Add [DataMemberIgnore] to let Stride Ignore the Member in the [DataContract] or change the get; accesibility.",
            helpLinkUri: "https://www.stride3d.net"
        );
        Location location = Location.Create(classInfo.TypeSyntax.SyntaxTree, property.DeclaringSyntaxReferences.FirstOrDefault().Span);
        classInfo.ExecutionContext.ReportDiagnostic(Diagnostic.Create(error, location));
    }
    private bool HasProperAccess(IPropertySymbol propertyInfo)
    {
        if (propertyInfo == null)
            return false;
        return (propertyInfo.SetMethod?.DeclaredAccessibility == Accessibility.Public ||
                propertyInfo.SetMethod?.DeclaredAccessibility == Accessibility.Internal ||
                propertyInfo.GetMethod?.ReturnsVoid == true
            )
            &&
                (propertyInfo.GetMethod?.DeclaredAccessibility == Accessibility.Public ||
                propertyInfo.GetMethod?.DeclaredAccessibility == Accessibility.Internal);
    }
}
