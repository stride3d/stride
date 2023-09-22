using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

public class CollectionAccessReporter : IViolationReporter
{
    public ClassInfo ClassInfo { get; set; }

    public void ReportViolation(ISymbol baseType, ClassInfo classInfo)
    {
        if (!CanHandle(baseType))
        {
            return;
        }
        if (IsValid(baseType))
        {
            return;
        }
        IPropertySymbol property = baseType as IPropertySymbol;
        Report(property, classInfo);
    }
    public bool CanHandle(ISymbol classMember)
    {
        if (classMember is IPropertySymbol property && PropertyHelper.IsICollection_generic(property.Type) && !PropertyHelper.IsDictionary(property, ClassInfo) && !PropertyHelper.IsArray(property) && !this.ShouldBeIgnored(property))
        {
            return true;
        }
        return false;
    }
    public bool IsValid(ISymbol classMember)
    {
        IPropertySymbol property = classMember as IPropertySymbol;
        if (HasProperAccess(property))
        {
            return true;
        }
        return false;
    }
    private static void Report(IPropertySymbol property, ClassInfo classInfo)
    {
        var error = new DiagnosticDescriptor(
            id: ErrorCodes.InvalidCollectionAccess,
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
