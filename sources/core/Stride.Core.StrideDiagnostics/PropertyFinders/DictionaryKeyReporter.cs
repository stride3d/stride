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
            var firstTypeArgument = ((INamedTypeSymbol)property.Type).TypeArguments[0];
            if (IsPrimitiveType(firstTypeArgument))
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
        // List of all C# primitive types (both aliased and non-aliased names)
        var primitiveTypes = new HashSet<string>
        {
            "bool",
            "Boolean",
            "byte",
            "Byte",
            "sbyte",
            "SByte",
            "char",
            "Char",
            "decimal",
            "Decimal",
            "double",
            "Double",
            "float",
            "Single", // Note that Single is the non-aliased name for float.
            "int",
            "Int32", // Note that Int32 is the non-aliased name for int.
            "uint",
            "UInt32", // Note that UInt32 is the non-aliased name for uint.
            "long",
            "Int64", // Note that Int64 is the non-aliased name for long.
            "ulong",
            "UInt64", // Note that UInt64 is the non-aliased name for ulong.
            "short",
            "Int16", // Note that Int16 is the non-aliased name for short.
            "ushort",
            "UInt16", // Note that UInt16 is the non-aliased name for ushort.
            "string",
            "String"
        };

        return primitiveTypes.Contains(type.Name);
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