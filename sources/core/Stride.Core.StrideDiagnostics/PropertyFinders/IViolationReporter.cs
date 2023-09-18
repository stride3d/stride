using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

public interface IViolationReporter
{
    public void ReportViolations(ref INamedTypeSymbol baseType, ClassInfo classInfo);
}