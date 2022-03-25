using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Core.CompilerServices
{
    public partial class SerializerGenerator
    {
        private const string CompilerServicesDiagnosticIdFormat = "STR0{0:000}";
        private const string CompilerServicesDiagnosticCategory = "CompilerServices";

        private static DiagnosticDescriptor CompilerServicesUnhandledException = new DiagnosticDescriptor(
            string.Format(CompilerServicesDiagnosticIdFormat, 1),
            "An unhandled exception occurred",
            "An {0} occurred while running Stride.Core.CompilerServices analyzer. {1}",
            CompilerServicesDiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor CompilerServicesExceptionDuringDataContractGeneration = new DiagnosticDescriptor(
            string.Format(CompilerServicesDiagnosticIdFormat, 2),
            "An exception occurred while emitting a DataContract class serializer",
            "An {1} occurred while emitting a DataContract class serializer for class {0}. {2}",
            CompilerServicesDiagnosticCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

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

        private static DiagnosticDescriptor DataContractMemberHasNonSerializableType = new DiagnosticDescriptor(
            string.Format(DataContractDiagnosticIdFormat, 3),
            "Member type is not serializable",
            "Member {0} of class {1} is of type {2} that cannot be serialized. Add [DataMemberIgnore] to suppress this warning.",
            DataContractDiagnosticCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
