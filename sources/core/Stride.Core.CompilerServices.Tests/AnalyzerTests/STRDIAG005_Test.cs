using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG005_Test
{
    [Fact]
    public void Error_On_string_readonly_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public string Value { get; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG005ReadonlyMemberTypeIsNotSupported.DiagnosticId);
    }

    [Fact]
    public void Error_On_string_readonly_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public readonly string Value;");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG005ReadonlyMemberTypeIsNotSupported.DiagnosticId);
    }

    [Fact]
    public void Error_On_int_readonly_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public int Value { get; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG005ReadonlyMemberTypeIsNotSupported.DiagnosticId);
    }

    [Fact]
    public void Error_On_int_readonly_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public readonly int Value;");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG005ReadonlyMemberTypeIsNotSupported.DiagnosticId);
    }
}
