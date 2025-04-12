using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG004_Test
{
    [Fact]
    public async Task Error_On_No_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { set; }");
        await TestHelper.ExpectDiagnosticsErrorAsync(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_private_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { private get; set; }");
        await TestHelper.ExpectDiagnosticsErrorAsync(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_protected_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { protected get; set; }");
        await TestHelper.ExpectDiagnosticsErrorAsync(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_private_protected_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { private protected get; set; }");
        await TestHelper.ExpectDiagnosticsErrorAsync(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }
}
