using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.Tests;

public class DoubleAnnotationError
{
    [Fact]
    public void HasDataMemberAndDataMemberIgnoreAtTheSameTime()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class DoubleAnnotation
{
    [DataMemberIgnore]
    [DataMember]
    public int Value { get; set; }
}
";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(x => x.Id == ErrorCodes.DoubledAnnotation);

        // Assert that there is an error
        Assert.True(hasError, "The Dictionary Key should be invalid.");
    }
}

