using Microsoft.CodeAnalysis;
using System.Runtime.Serialization;
using Xunit;

namespace Stride.Core.StrideDiagnostics.Tests;

public class ArrayError
{
    [Fact]
    public void ErrorOnInvalidArrayAccess()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
using System.Runtime.Serialization;

[DataContract]
public class ArrayError
{
    public ArrayError[] Array { private get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Warning &&
            diagnostic.Id == "STRD001");

        // Assert that there is an error
        Assert.True(hasError, "The 'Array' property should generate an error.");

    }
    [Fact]
    public void IgnoreMember1()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
using System.Runtime.Serialization;

[DataContract]
public class IgnoreArray
{
    [DataMemberIgnore]
    public ArrayError[] Array { get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Array should be ignored but wasn't.");

    }
    [Fact]
    public void IgnoreMember2()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
using System.Runtime.Serialization;

[DataContract]
public class IgnoreArray
{
    private ArrayError[] Array { get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Array should be ignored but wasn't.");

    }
}
