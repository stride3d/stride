using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using StrideDiagnostics;
using System.Reflection.Metadata;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Stride.Core.StrideDiagnostics.Tests.StrideDiganostics;
public class STRDIAG003UnitTests
{

    private readonly DiagnosticAnalyzer analyzer = new STRDIAG003InaccessibleMember(); // Replace with your analyzer class

    private const string ExpectedDiagnosticId = STRDIAG003InaccessibleMember.DiagnosticId; // Replace with the specific diagnostic ID you're testing

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/61851")]
    public void DataMemberOnPrivateField()
    {
        var testCode = @"
using Stride.Core;

public class MyClass
{
    [DataMember]
    private int myField;
}";
        var compilation = DIAGTestHelper.CreateCompilation(testCode);
        var diagnostics = DIAGTestHelper.AnalyzeCompilation(compilation, analyzer);
        var expectedDiagnostic = diagnostics.First(d => d.Id == ExpectedDiagnosticId);
        Assert.NotNull(expectedDiagnostic);
    }
    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/61851")]
    public void DataMemberOnPrivateProperty()
    {
        var testCode = @"
using Stride.Core;

public class MyClass
{
    [Stride.Core.DataMember]
    private int myField {get;set;}
}";
        var compilation = DIAGTestHelper.CreateCompilation(testCode);
        var diagnostics = DIAGTestHelper.AnalyzeCompilation(compilation, analyzer);
        var expectedDiagnostic = diagnostics.FirstOrDefault(d => d.Id == ExpectedDiagnosticId);
        Assert.NotNull(expectedDiagnostic);
    }
}

