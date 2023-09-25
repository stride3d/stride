using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using StrideDiagnostics;

namespace Stride.Core.StrideDiagnostics.Tests.StrideDiganostics;
public class STRDIAG004
{
    private readonly DiagnosticAnalyzer analyzer = new STRDIAG004MemberWithNoGetter();
    private const string ExpectedDiagnosticId = STRDIAG004MemberWithNoGetter.DiagnosticId;


    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/61851")]
    public void PrivateFieldWithDataMember()
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
}
