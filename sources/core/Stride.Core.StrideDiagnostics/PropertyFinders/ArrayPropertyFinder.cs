using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

public class ArrayPropertyFinder : IViolationReporter
{
    public ClassInfo ClassInfo { get; set; }

    public void ReportViolation(ISymbol classMember, ClassInfo info)
    {
        if (!CanHandle(classMember))
        {
            return;
        }
        if (IsValid(classMember))
        {
            return;
        }
        IPropertySymbol property = classMember as IPropertySymbol;
        Report(property, info);
    }
    public bool CanHandle(ISymbol classMember)
    {
        if (classMember is IPropertySymbol property && PropertyHelper.IsArray(property) && !this.ShouldBeIgnored(property))
        {
            return true;
        }
        return false;
    }
    public bool IsValid(ISymbol classMember)
    {
        if (classMember is IPropertySymbol property && !HasProperAccess(property))
        {
            return false;
        }
        return true;
    }
    private static void Report(IPropertySymbol property, ClassInfo classInfo)
    {
        var error = new DiagnosticDescriptor(
            id: ErrorCodes.InvalidArrayAccess,
            title: "Invalid Access",
            category: NexGenerator.CompilerServicesDiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            messageFormat: $"The Property '{property.Name}' has an invalid Access Type for an Array, expected for Arrays is a public/internal get; Accessor, Stride will not be able to use this Property as [DataMember]. Add [DataMemberIgnore] to let Stride Ignore the Member in the [DataContract] or change the get; accesibility.",
            helpLinkUri: ""
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
