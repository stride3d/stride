using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class STRDIAG001_Test
{
    [Fact]
    public void Error_On_Private_Inner_Class_with_DataContract()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataContract] private class InnerClass { }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG001InvalidDataContract.DiagnosticId);
    }
    [Fact]
    public void No_Error_On_Private_Inner_Class_without_DataContract()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "private class InnerClass { }");
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
    // TODO: Enable with .NET8 merge as we need a higher C# version
    [Fact(Skip = "file scoped classes won't compile")]
    public void Error_On_file_scope_Class_with_DataContract()
    {
        string sourceCode = "using Stride.Core; [DataContract] file class FileScopeClass { }";
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG001InvalidDataContract.DiagnosticId);
    }
    [Fact(Skip = "file scoped classes won't compile")]
    public void No_Error_On_file_scope_Class_without_DataContract()
    {
        string sourceCode = "using Stride.Core; file class FileScopeClass { }";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
}
