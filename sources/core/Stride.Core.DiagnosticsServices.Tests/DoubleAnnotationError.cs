using Microsoft.CodeAnalysis;
using Stride.Core;
using StrideDiagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace StrideDiagnosticsTests;
public class DoubleAnnotationError
{
    [Fact]
    public void HasDataMemberAndDataMemberIgnoreAtTheSameTime()
    {
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
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(x => x.Id == ErrorCodes.DoubledAnnotation);

        // Assert that there is an error
        Assert.True(hasError, "The Dictionary Key should be invalid.");
    }
}

