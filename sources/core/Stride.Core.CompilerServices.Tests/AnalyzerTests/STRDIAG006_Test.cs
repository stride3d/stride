using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class STRDIAG006_Test
{
    [Fact]
    public async Task Error_On_No_Set_AssignMode()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Assign)] public int Value { get; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG006InvalidAssignMode.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_private_Set_AssignMode()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Assign)] public int Value {  get; private set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG006InvalidAssignMode.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_protected_Set_AssignMode()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Assign)] public int Value {  get; protected set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG006InvalidAssignMode.DiagnosticId);

    }

    [Fact]
    public async Task Error_On_private_protected_Set_AssignMode()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Assign)] public int Value {  get; private protected set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG006InvalidAssignMode.DiagnosticId);
    }
}
