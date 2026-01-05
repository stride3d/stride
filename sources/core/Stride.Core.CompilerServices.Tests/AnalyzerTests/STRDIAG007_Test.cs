using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG007_Test
{
    [Fact]
    public async Task Error_On_DataMembered_Delegate_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public Action Value;");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG007DataMemberOnDelegate.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_DataMembered_Delegate_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public Action Value { get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG007DataMemberOnDelegate.DiagnosticId);
    }
}
