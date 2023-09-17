using Microsoft.CodeAnalysis;

namespace StrideDiagnostics.PropertyFinders;

public interface IViolationReporter
{
    /// <summary>
    /// Reports a Diagnostics Error/Warning when a Stride "rule" is broken.
    /// </summary>
    /// <param name="baseType">The semantic Type to analyze</param>
    /// <param name="classInfo">General Execution info</param>
    public void ReportViolations(ref INamedTypeSymbol baseType, ClassInfo classInfo);
}
