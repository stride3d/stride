using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace StrideDiagnostics.PropertyFinders;
public class ArrayPropertyFinder : IViolationReporter, IPropertyFinder
{
    /// <summary>
    /// Finds and returns a collection of properties declared in the specified base type and its derived types.
    /// Ignores <see cref="PropertyAttributeFinderExtension.ShouldBeIgnored(IPropertyFinder, IPropertySymbol)"/> Properties.
    /// </summary>
    /// <param name="baseType">The <see cref="INamedTypeSymbol"/> representing the base type to search for properties.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="IPropertySymbol"/> representing the properties
    /// declared in the specified base type and its derived types.
    /// </returns>
    /// <remarks>
    /// This method searches for properties declared in the specified base type and its derived types.
    /// It returns an enumerable collection of <see cref="IPropertySymbol"/> representing these properties.
    /// </remarks>
    public IEnumerable<IPropertySymbol> Find(ref INamedTypeSymbol baseType)
    {
        if (baseType == null)
            return Enumerable.Empty<IPropertySymbol>();
        return baseType.GetMembers().OfType<IPropertySymbol>().Where(property => PropertyHelper.IsArray(property) && !this.ShouldBeIgnored(property) && HasProperAccess(property));
    }

    private bool HasProperAccess(IPropertySymbol property)
    {
        return property.GetMethod?.DeclaredAccessibility == Accessibility.Public ||
                property.GetMethod?.DeclaredAccessibility == Accessibility.Internal;
    }

    public void ReportViolations(ref INamedTypeSymbol baseType, ClassInfo info)
    {
        if (baseType == null)
            return;
        IEnumerable<IPropertySymbol> violations = baseType.GetMembers().OfType<IPropertySymbol>().Where(property => PropertyHelper.IsArray(property) && !this.ShouldBeIgnored(property) && !HasProperAccess(property));
        foreach (IPropertySymbol violation in violations)
        {
            Report(violation, info);
        }
    }
    private static void Report(IPropertySymbol property, ClassInfo classInfo)
    {
        DiagnosticDescriptor error = new DiagnosticDescriptor(
            id: ErrorCodes.ArrayAccess,
            title: "Invalid Access",
            category: NexGenerator.CompilerServicesDiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            messageFormat: $"The Property '{property.Name}' has an invalid Access Type for an Array, expected for Arrays is a public/internal get; Accessor, Stride will not be able to use this Property as [DataMember]. Add [DataMemberIgnore] to let Stride Ignore the Member in the [DataContract] or change the get; accesibility.",
            helpLinkUri: "https://www.stride3d.net"
        );
        Location location = Location.Create(classInfo.TypeSyntax.SyntaxTree, property.DeclaringSyntaxReferences.FirstOrDefault().Span);
        classInfo.ExecutionContext.ReportDiagnostic(Diagnostic.Create(error, location));
    }
}
