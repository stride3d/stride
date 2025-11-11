using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

/// <summary>
/// Tests for <see cref="STRDIAG003InaccessibleMember"/> analyzer.
/// Validates that DataMember attribute is only applied to accessible members.
/// </summary>
public class STRDIAG003_Test
{
    [Fact]
    public async Task Error_On_Datamember_With_private_InaccessibleMember_On_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] private int Value { get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG003InaccessibleMember.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_Datamember_With_private_InaccessibleMember_On_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] private int Value = 0;");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG003InaccessibleMember.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_Datamember_With_protected_InaccessibleMember_On_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] protected int Value { get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG003InaccessibleMember.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_Datamember_With_protected_InaccessibleMember_On_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] protected int Value = 0;");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG003InaccessibleMember.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_Datamember_With_private_protected_InaccessibleMember_On_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] private protected int Value { get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG003InaccessibleMember.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_Datamember_With_private_protected_InaccessibleMember_On_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] private protected int Value = 0;");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG003InaccessibleMember.DiagnosticId);
    }

    [Fact]
    public async Task NoError_On_Public_Member()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_Internal_Member()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] internal int Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_ProtectedInternal_Member()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] protected internal int Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
}
