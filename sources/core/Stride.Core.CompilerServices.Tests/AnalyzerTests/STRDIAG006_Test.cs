using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class STRDIAG006_Test
{
    [Fact]
    public void Error_On_No_Set_AssignMode()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Assign)] public int Value { get; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG006InvalidAssignMode.DiagnosticId);
    }

    [Fact]
    public void Error_On_private_Set_AssignMode()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Assign)] public int Value {  get; private set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG006InvalidAssignMode.DiagnosticId);
    }

    [Fact]
    public void Error_On_protected_Set_AssignMode()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Assign)] public int Value {  get; protected set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG006InvalidAssignMode.DiagnosticId);

    }

    [Fact]
    public void Error_On_private_protected_Set_AssignMode()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember(DataMemberMode.Assign)] public int Value {  get; private protected set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG006InvalidAssignMode.DiagnosticId);
    }
}
