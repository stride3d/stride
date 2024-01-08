using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG002_Test
{
    [Fact]
    public void Error_On_Attribute_Contradiction_On_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Content)]public int Value { get; set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG002InvalidContentMode.DiagnosticId);
    }
}
