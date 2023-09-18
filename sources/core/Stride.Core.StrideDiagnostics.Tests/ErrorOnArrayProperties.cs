using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.Tests;

public class ErrorOnArrayProperties
{
    [Fact]
    public void Error_On_Private_Getter_of_Array_Property()
    {
        var sourceCode = @"
using Stride.Core;

[DataContract]
public class ArrayError
{
    public ArrayError[] Array { private get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Warning &&
            diagnostic.Id == ErrorCodes.InvalidArrayAccess);
        Assert.True(hasError, "The 'Array' property should generate an error, a private getter is not allowed.");

    }
    [Fact]
    public void DataMemberIgnore_Attribute_On_Arrays()
    {
        var sourceCode = @"
using Stride.Core;

[DataContract]
public class IgnoreArray
{
    [DataMemberIgnore]
    public ArrayError[] Array { get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any();
        Assert.False(hasError, "The Array should be ignored but wasn't.");
    }
    [Fact]
    public void Ignore_Private_Array_Property()
    {
        var sourceCode = @"
using Stride.Core;

[DataContract]
public class IgnoreArray
{
    private ArrayError[] Array { get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any();
        Assert.False(hasError, "The Array should be ignored but wasn't.");
    }
}
