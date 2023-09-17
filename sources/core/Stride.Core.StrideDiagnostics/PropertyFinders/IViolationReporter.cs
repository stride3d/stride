using Microsoft.CodeAnalysis;

namespace StrideDiagnostics.PropertyFinders;

public interface IViolationReporter
{
    public void ReportViolations(ref INamedTypeSymbol baseType, ClassInfo classInfo);
}