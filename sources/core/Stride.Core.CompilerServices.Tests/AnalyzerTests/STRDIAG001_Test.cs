using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class STRDIAG001_Test
{
    [Fact]
    public async Task Error_On_Private_Inner_Class_with_DataContract()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataContract] private class InnerClass { }");
        await TestHelper.ExpectDiagnosticsErrorAsync(sourceCode, STRDIAG001InvalidDataContract.DiagnosticId);
    }
    [Fact]
    public async Task No_Error_On_Private_Inner_Class_without_DataContract()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "private class InnerClass { }");
        await TestHelper.ExpectNoDiagnosticsErrorsAsync(sourceCode);
    }
    [Fact]
    public async Task Error_On_file_scope_Class_with_DataContract()
    {
        string sourceCode = "using Stride.Core; [DataContract] file class FileScopeClass { }";
        await TestHelper.ExpectDiagnosticsErrorAsync(sourceCode, STRDIAG001InvalidDataContract.DiagnosticId);
    }
    [Fact]
    public async Task No_Error_On_file_scope_Class_without_DataContract()
    {
        string sourceCode = "using Stride.Core; file class FileScopeClass { }";
        await TestHelper.ExpectNoDiagnosticsErrorsAsync(sourceCode);
    }
}
