using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

using System.Runtime.Serialization;
using Stride.Core;

public class STRDIAG000_Test
{
    [Fact]
    public void Error_On_Attribute_Contradiction()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class DoubleAnnotation
{
    [DataMemberIgnore]
    [DataMember]
    public int Value { get; set; }
}
";
        Assert.NotEmpty(CompilerUtils.AllAnalyzers);
        var analyzer = new STRDIAG000AttributeContradiction();
        var diagnostics = CompilerUtils.CompileAndGetAnalyzerDiagnostics(sourceCode,analyzer);
        bool hasError = diagnostics.Any(diagnostic => diagnostic.Id == STRDIAG000AttributeContradiction.DiagnosticId);

        Assert.True(hasError, $"The property should generate {STRDIAG000AttributeContradiction.DiagnosticId} as Diagnostic. Thrown Diagnostics: {diagnostics.Select(x => x.Id)}");
    }
}
