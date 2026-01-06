using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

/// <summary>
/// Tests for <see cref="STRDIAG001InvalidDataContract"/> analyzer.
/// Validates that DataContract attribute is only applied to public/internal types.
/// </summary>
public class STRDIAG001_Test
{
    [Fact]
    public async Task Error_On_Private_Inner_Class_with_DataContract()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataContract] private class InnerClass { }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG001InvalidDataContract.DiagnosticId);
    }

    [Fact]
    public async Task No_Error_On_Private_Inner_Class_without_DataContract()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "private class InnerClass { }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task Error_On_file_scope_Class_with_DataContract()
    {
        string sourceCode = "using Stride.Core; [DataContract] file class FileScopeClass { }";
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG001InvalidDataContract.DiagnosticId);
    }

    [Fact]
    public async Task No_Error_On_file_scope_Class_without_DataContract()
    {
        string sourceCode = "using Stride.Core; file class FileScopeClass { }";
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task No_Error_On_Public_Class_with_DataContract()
    {
        string sourceCode = "using Stride.Core; [DataContract] public class PublicClass { }";
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task No_Error_On_Internal_Class_with_DataContract()
    {
        string sourceCode = "using Stride.Core; [DataContract] internal class InternalClass { }";
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task Error_On_Protected_Inner_Class_with_DataContract()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataContract] protected class InnerClass { }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG001InvalidDataContract.DiagnosticId);
    }

    [Fact]
    public async Task No_Error_On_Public_Struct_with_DataContract()
    {
        string sourceCode = "using Stride.Core; [DataContract] public struct PublicStruct { }";
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task No_Error_On_Public_Record_with_DataContract()
    {
        string sourceCode = "using Stride.Core; [DataContract] public record PublicRecord { }";
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
}
