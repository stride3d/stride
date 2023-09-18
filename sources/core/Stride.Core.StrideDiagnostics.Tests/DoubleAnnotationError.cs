using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.Tests;

public class DoubleAnnotationError
{
    [Fact]
    public void Has_DataMember_and_DataMemberIgnore_At_The_Same_Time()
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
        var hasError = generatedDiagnostics.Any(x => x.Id == ErrorCodes.DoubledAnnotation);
        Assert.True(hasError, "The Dictionary can't be Ignored and evaluated at the same time.");
    }
}

