using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

/// <summary>
/// Tests for <see cref="STRDIAG002InvalidContentMode"/> analyzer.
/// Validates that DataMemberMode.Content is only used with mutable reference types.
/// </summary>
public class STRDIAG002_Test
{
    [Fact]
    public async Task Error_On_ContentMode_With_ValueType_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Content)]public int Value { get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG002InvalidContentMode.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_ContentMode_With_ValueType_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Content)]public int Value;");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG002InvalidContentMode.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_ContentMode_With_String_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Content)]public string Value { get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG002InvalidContentMode.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_ContentMode_With_Struct_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "public struct MyStruct { } [DataMember(DataMemberMode.Content)]public MyStruct Value { get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG002InvalidContentMode.DiagnosticId);
    }

    [Fact]
    public async Task NoError_On_ContentMode_With_ReferenceType_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Content)]public object Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_AssignMode_With_ValueType()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Assign)]public int Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_DefaultMode_With_ValueType()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember]public int Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
}
