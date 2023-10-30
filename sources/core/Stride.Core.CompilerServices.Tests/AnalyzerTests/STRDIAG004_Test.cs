using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG004_Test
{
    [Fact]
    public void Error_On_No_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public void Error_On_private_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { private get; set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public void Error_On_protected_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { protected get; set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }

    [Fact]
    public void Error_On_private_protected_Get_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { private protected get; set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG004PropertyWithNoGetter.DiagnosticId);
    }
}
