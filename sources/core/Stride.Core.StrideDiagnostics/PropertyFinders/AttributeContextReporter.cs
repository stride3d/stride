using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

internal class AttributeContextReporter : IViolationReporter
{
    public ClassInfo ClassInfo { get; set; }

    public void ReportViolation(IPropertySymbol classMember, ClassInfo classInfo)
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
        Report(property, classInfo);
    }
    public bool IsValid(IPropertySymbol property)
    {
        if (this.ShouldBeIgnored(property) && this.HasDataMemberAnnotation(property))
        {
            return false;
        }
        return true;
    }
    public bool CanHandle(IPropertySymbol classMember)
    {
        return true;
    }
    private static void Report(IPropertySymbol property, ClassInfo classInfo)
    {
        var error = new DiagnosticDescriptor(
            id: ErrorCodes.InvalidDataMemberCombination,
            title: "Invalid Annotations",
            category: NexGenerator.CompilerServicesDiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            messageFormat: $"The Property has a contradiction in the Annotations, there can't be [DataMember] and [DataMemberIgnore] on the same Property.\nIt's also not allowed to annotate DataMember on a Property that Stride will ignore ( private/protected ).",
            helpLinkUri: "https://www.stride3d.net"
        );
        var location = Location.Create(classInfo.TypeSyntax.SyntaxTree, property.DeclaringSyntaxReferences.FirstOrDefault().Span);
        classInfo.ExecutionContext.ReportDiagnostic(Diagnostic.Create(error, location));
    }
}
