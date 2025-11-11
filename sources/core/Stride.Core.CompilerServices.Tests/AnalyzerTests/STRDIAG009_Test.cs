using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class STRDIAG009_Test
{
    private static readonly string[] Types =
    [
        "string",
        "int",
        "float",
        "double",
    ];
    [Fact]
    public async Task No_Error_On_Immutable_Types()
    {
        foreach(var type in Types)
        {
            string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, $"[DataMember] public System.Collections.Generic.Dictionary<{type},object> Value {{ get; }}");
            await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
        }
    }
    [Fact]
    public async Task No_Error_On_Enum_As_Key()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, $"public enum TestEnumKey {{ Yes,No }}[DataMember] public System.Collections.Generic.Dictionary<TestEnumKey,object> Value {{ get; }}");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
    [Fact]
    public async Task Error_On_Reference_Type()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public System.Collections.Generic.Dictionary<object,object> Value { get; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG009InvalidDictionaryKey.DiagnosticId);
    }
    [Fact]
    public async Task Error_On_Interface_Type()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember] public System.Collections.Generic.Dictionary<ICloneable,object> Value { get; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG009InvalidDictionaryKey.DiagnosticId);
    }
}
