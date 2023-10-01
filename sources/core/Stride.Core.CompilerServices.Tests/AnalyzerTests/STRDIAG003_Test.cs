using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class STRDIAG003_Test
{
    [Fact]
    public void Error_On_InaccessibleMember_On_Property()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class DoubleAnnotation
{
    [DataMember]
    private int Value { get; set; }
}
";
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG003InaccessibleMember.DiagnosticId);
    }
    [Fact]
    public void Error_On_InaccessibleMember_On_Field()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class DoubleAnnotation
{
    [DataMember]
    private int Value;
}
";
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG003InaccessibleMember.DiagnosticId);
    }
}
