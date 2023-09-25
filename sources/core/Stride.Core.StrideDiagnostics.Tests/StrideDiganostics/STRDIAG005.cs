using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Stride.Core.StrideDiagnostics.StrideDiagnostics;
using StrideDiagnostics;

namespace Stride.Core.StrideDiagnostics.Tests.StrideDiganostics;
public class STRDIAG005
{
    private readonly DiagnosticAnalyzer analyzer = new STRDIAG005ReadonlyMemberIsReferenceType();
    private const string ExpectedDiagnosticId = STRDIAG005ReadonlyMemberIsReferenceType.DiagnosticId;


    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/61851")]
    public void ReadonlyFieldStructWithDatamember()
    {
        var testCode = @"
using Stride.Core;
public class MyClass
{
    [Stride.Core.DataMember]
    private readonly int myField;
}";

        var compilation = DIAGTestHelper.CreateCompilation(testCode);
        var diagnostics = DIAGTestHelper.AnalyzeCompilation(compilation, analyzer);
        var expectedDiagnostic = diagnostics.First(d => d.Id == ExpectedDiagnosticId);
        Assert.NotNull(expectedDiagnostic);
    }
    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/61851")]
    public void ReadonlyPropertyStructWithDatamember()
    {
        var testCode = @"
using Stride.Core;
public class MyClass
{
    [Stride.Core.DataMember]
    private int myField { get; }
}";

        var compilation = DIAGTestHelper.CreateCompilation(testCode);
        var diagnostics = DIAGTestHelper.AnalyzeCompilation(compilation, analyzer);
        var expectedDiagnostic = diagnostics.FirstOrDefault(d => d.Id == ExpectedDiagnosticId);
        Assert.NotNull(expectedDiagnostic);
    }
    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/61851")]
    public void ReadonlyPropertyStringWithDatamember()
    {
        var testCode = @"
using Stride.Core;
public class MyClass
{
    [Stride.Core.DataMember]
    private string myField { get; }
}";

        var compilation = DIAGTestHelper.CreateCompilation(testCode);
        var diagnostics = DIAGTestHelper.AnalyzeCompilation(compilation, analyzer);
        var expectedDiagnostic = diagnostics.FirstOrDefault(d => d.Id == ExpectedDiagnosticId);
        Assert.NotNull(expectedDiagnostic);
    }
    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/61851")]
    public void ReadonlyFieldStringWithDatamember()
    {
        var testCode = @"
using Stride.Core;
public class MyClass
{
    [Stride.Core.DataMember]
    private readonly string myField;
}";

        var compilation = DIAGTestHelper.CreateCompilation(testCode);
        var diagnostics = DIAGTestHelper.AnalyzeCompilation(compilation, analyzer);
        var expectedDiagnostic = diagnostics.First(d => d.Id == ExpectedDiagnosticId);
        Assert.NotNull(expectedDiagnostic);
    }
}
