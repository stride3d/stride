using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;
public class DictionaryAccessReporter : IViolationReporter
{
    public ClassInfo ClassInfo { get; set; }

    public void ReportViolation(IPropertySymbol property, ClassInfo classInfo)
    {
        if (!CanHandle(property))
        {
            return;
        }
        if (IsValid(property))
        {
            return;
        }
        Report(property, classInfo);
    }
    public bool CanHandle(IPropertySymbol property)
    {
        if (PropertyHelper.IsDictionary(property, ClassInfo) && !PropertyHelper.IsArray(property) && !this.ShouldBeIgnored(property))
        {
            return true;
        }
        return false;
    }
    public bool IsValid(IPropertySymbol property)
    {
        if (HasProperAccess(property))
        {
            return true;
        }
        return false;
    }
    private static void Report(IPropertySymbol property, ClassInfo classInfo)
    {
        var error = new DiagnosticDescriptor(
            id: ErrorCodes.InvalidDictionaryAccess,
            title: "Invalid Access",
            category: NexGenerator.CompilerServicesDiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            messageFormat: $"The Property '{property.Name}' has an invalid Access Type for an {property.Type}, expected for a Dictionary<T,T> is a public/internal get; Accessor, Stride will not be able to use this Property as [DataMember]. Add [DataMemberIgnore] to let Stride Ignore the Member in the [DataContract] or change the get; accesibility.",
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