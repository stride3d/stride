using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

public class PropertyAccessReporter : IViolationReporter
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
        if (classMember is IPropertySymbol property && !PropertyHelper.IsArray(property) && !PropertyHelper.IsICollection_generic(property.Type) && !this.ShouldBeIgnored(property) && !PropertyHelper.IsDictionary(property, ClassInfo))
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
            id: ErrorCodes.InvalidPropertyAccess,
            title: "Invalid Access",
            category: NexGenerator.CompilerServicesDiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            messageFormat: $"The Property '{property.Name}' has an invalid Access Type for a Property, expected for non Collection/Array is a public/internal get; and a public/internal set;/init; Accessor, Stride will not be able to use this Property as [DataMember]. Add [DataMemberIgnore] to let Stride Ignore the Member in the [DataContract] or change the get; accesibility.",
            helpLinkUri: "https://www.stride3d.net"
        );
        var location = Location.Create(classInfo.TypeSyntax.SyntaxTree, property.DeclaringSyntaxReferences.FirstOrDefault().Span);
        classInfo.ExecutionContext.ReportDiagnostic(Diagnostic.Create(error, location));
    }
    private bool HasProperAccess(IPropertySymbol propertyInfo)
    {
        if (propertyInfo == null)
            return false;
        return HasPublicInternalSetterGetters(propertyInfo);
    }

    private static bool HasPublicInternalSetterGetters(IPropertySymbol propertyInfo)
    {
        return (propertyInfo.SetMethod?.DeclaredAccessibility == Accessibility.Public ||
                propertyInfo.SetMethod?.DeclaredAccessibility == Accessibility.Internal)
                    &&
               (propertyInfo.GetMethod?.DeclaredAccessibility == Accessibility.Public ||
                propertyInfo.GetMethod?.DeclaredAccessibility == Accessibility.Internal);
    }
}
