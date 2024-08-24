using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class STRDIAG010_Test
{
    [Fact]
    public void No_Error_On_Default_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, " ");
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
    [Fact]
    public void No_Error_On_Empty_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "public ValidCollection() { }");
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
    [Fact]
    public void No_Error_On_Empty_And_Other_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "public ValidCollection() { } public ValidCollection(int x) { }");
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
    [Fact]
    public void Error_On_No_Empty_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "public ValidCollection(int x) { }");
        TestHelper.ExpectDiagnosticsError(sourceCode,STRDIAG010InvalidConstructor.DiagnosticId);
    }
    [Fact]
    public void Error_On_DataContract_Inherited()
    {
        string sourceCode = string.Format(ClassTemplates.InheritedDataContract, "public Inherited(int x) { }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG010InvalidConstructor.DiagnosticId);
    }
    [Fact]
    public void Error_On_Primary_Constructor()
    {
        string sourceCode = string.Format(ClassTemplates.PrimaryConstructorTemplate, "");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG010InvalidConstructor.DiagnosticId);
    }
    [Fact]
    public void No_Error_On_Flipped_DataContract_Parameters()
    {
        string sourceCode = string.Format(ClassTemplates.DataContractArgumentsTemplate, "");
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
}
