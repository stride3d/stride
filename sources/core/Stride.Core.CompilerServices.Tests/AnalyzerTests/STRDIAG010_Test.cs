using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class STRDIAG010_Test
{
    [Fact]
    public async Task No_Error_On_Default_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, " ");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
    [Fact]
    public async Task No_Error_On_Empty_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "public ValidCollection() { }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
    [Fact]
    public async Task No_Error_On_Empty_And_Other_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "public ValidCollection() { } public ValidCollection(int x) { }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
    [Fact]
    public async Task Error_On_No_Empty_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "public ValidCollection(int x) { }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode,STRDIAG010InvalidConstructor.DiagnosticId);
    }
    [Fact]
    public async Task Error_On_DataContract_Inherited()
    {
        string sourceCode = string.Format(ClassTemplates.InheritedDataContract, "public Inherited(int x) { }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG010InvalidConstructor.DiagnosticId);
    }
    [Fact]
    public async Task Error_On_Primary_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.PrimaryConstructorTemplate, "");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG010InvalidConstructor.DiagnosticId);
    }
    [Fact]
    public async Task No_Error_On_Flipped_DataContract_Parameters()
    {
        string sourceCode = string.Format(ClassTemplates.DataContractArgumentsTemplate, "");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
}
