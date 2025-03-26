using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG002_Test
{
    [Fact]
    public async Task Error_On_Attribute_Contradiction_On_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Content)]public int Value { get; set; }");
        await TestHelper.ExpectDiagnosticsErrorAsync(sourceCode, STRDIAG002InvalidContentMode.DiagnosticId);
    }
}
