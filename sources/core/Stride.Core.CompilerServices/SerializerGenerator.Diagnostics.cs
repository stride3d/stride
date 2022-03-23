using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Core.CompilerServices
{
    public partial class SerializerGenerator
    {
        private const string DataContractDiagnosticIdFormat = "STR2{0:000}";
        private const string DataContractDiagnosticCategory = "DataContract";

        private static DiagnosticDescriptor DataContractClassHasNoAccessibleParameterlessCtor = new DiagnosticDescriptor(
            string.Format(DataContractDiagnosticIdFormat, 1),
            "DataContract class has no accessible parameterless constructor",
            "{0} class has no public parameterless constructor that can be used for serialization.",
            DataContractDiagnosticCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor DataContractMemberHasBothIncludeAndIgnoreAttr = new DiagnosticDescriptor(
            string.Format(DataContractDiagnosticIdFormat, 2),
            "Member has both DataMember and DataMemberIgnore attributes",
            "Member {0} of class {1} has both DataMember and DataMemberIgnore attributes which makes it ambigous if it should be included for serialization.",
            DataContractDiagnosticCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
