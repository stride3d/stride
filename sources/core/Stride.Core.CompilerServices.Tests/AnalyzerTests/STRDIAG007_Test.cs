using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG007_Test
{
    [Fact]
    public void Error_On_DataMembered_Delegate_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public Action Value;");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG007DataMemberOnDelegate.DiagnosticId);
    }

    [Fact]
    public void Error_On_DataMembered_Delegate_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public Action Value { get; set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG007DataMemberOnDelegate.DiagnosticId);
    }
}
