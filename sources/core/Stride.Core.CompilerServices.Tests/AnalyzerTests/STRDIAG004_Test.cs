using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

/// <summary>
/// Tests for <see cref="STRDIAG004PropertyWithNoGetter"/> analyzer.
/// Validates that DataMember properties have accessible getters.
/// </summary>
public class STRDIAG004_Test
{
    [Fact]
    public async Task Error_On_No_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_private_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { private get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_protected_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { protected get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_private_protected_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { private protected get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public async Task NoError_On_Public_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_Internal_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { internal get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_ProtectedInternal_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { protected internal get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value;");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
}
