using Microsoft.CodeAnalysis;
using Stride.Core.StrideDiagnostics;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

public interface IViolationReporter
{
    public void ReportViolations(ref INamedTypeSymbol baseType, ClassInfo classInfo);
}