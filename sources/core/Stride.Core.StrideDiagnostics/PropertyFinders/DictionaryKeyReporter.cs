using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

internal class DictionaryKeyReporter : IViolationReporter
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
        if (PropertyHelper.IsDictionary(property, ClassInfo) && !this.ShouldBeIgnored(property))
        {
            return true;
        }
        return false;
    }
    public bool IsValid(IPropertySymbol property)
    {
        if (InvalidDictionaryKey(property, ClassInfo))
        {
            return false;
        }
        return true;
    }
    private bool InvalidDictionaryKey(IPropertySymbol property, ClassInfo info)
    {
        if (PropertyHelper.IsDictionary(property, info))
        {
            INamedTypeSymbol dictionaryInterface = info.ExecutionContext.Compilation.GetTypeByMetadataName(typeof(IDictionary<,>).FullName);
            SymbolEqualityComparer comparer = SymbolEqualityComparer.Default;
            var interfacly = ((INamedTypeSymbol)property.Type).AllInterfaces.First(x => x.OriginalDefinition.Equals(dictionaryInterface, comparer));
            if (IsPrimitiveType(interfacly.TypeArguments[0]))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        return false;
    }
    private bool IsPrimitiveType(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Char:
            case SpecialType.System_Int16:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt16:
            case SpecialType.System_UInt32:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_String:
            case SpecialType.System_Decimal:
                return true;
            default:
                return false;
        }
    }
    private void Report(IPropertySymbol property, ClassInfo classInfo)
    {
        var error = new DiagnosticDescriptor(
            id: ErrorCodes.InvalidDictionaryKey,
            title: "Invalid Dictionary Key",
            category: NexGenerator.CompilerServicesDiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            messageFormat: $"The Generic Key for '{property.Name}' is invalid, expected for a IDictionary<T,Y> is a struct/simple type Key to use this Property as [DataMember]. Add [DataMemberIgnore] to let Stride Ignore the Member in the [DataContract] or change the Dictionary Key.",
            helpLinkUri: "https://www.stride3d.net"
        );
        var location = Location.Create(classInfo.TypeSyntax.SyntaxTree, property.DeclaringSyntaxReferences.FirstOrDefault().Span);
        classInfo.ExecutionContext.ReportDiagnostic(Diagnostic.Create(error, location));

    }
}
