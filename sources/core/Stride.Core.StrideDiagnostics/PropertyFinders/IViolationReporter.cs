using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

public interface IViolationReporter
{
    public ClassInfo ClassInfo { get; set; }
    /// <summary>
    /// Reports violations associated with a class member.
    /// <see cref="IsValid(ref ISymbol)"/> must be called inside the Method.
    /// It's not needed to call it beforehand.
    /// </summary>
    /// <param name="classMember">
    /// A reference to the class member for which violations may be reported.
    /// </param>
    /// <param name="info">
    /// Additional information about the class.
    /// </param>
    public void ReportViolation(ISymbol classMember, ClassInfo classInfo);
    /// <summary>
    /// Decides if the <see cref="INamedTypeSymbol"/> can be handled by this <see cref="IViolationReporter"/>
    /// </summary>
    /// <param name="classMember">The member to analyze if it can be analyzed</param>
    /// <returns>true if it can be handled, else false</returns>
    public bool CanHandle(ISymbol classMember);
    /// <summary>
    /// Checks if the member is Valid in it's Declaration for Stride or not.
    /// <see cref="ReportViolation(ref ISymbol, ClassInfo)"/> will Report Diagnostics if it is not <see cref="IsValid(ref ISymbol)"/>
    /// The <see cref="CanHandle(ref ISymbol)"/> must be true so <see cref="IsValid(ref ISymbol)"/> can return a valid result.
    /// </summary>
    /// <param name="classMember">A reference to the class member</param>
    /// <returns>true if it's declaration is valid</returns>
    public bool IsValid(ISymbol classMember);
}
