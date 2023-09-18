using Microsoft.CodeAnalysis;
using Stride.Core;
using Xunit;

namespace StrideDiagnosticsTests;
public class PropertyError
{
    [Fact]
    public void ErrorOnInvalidPropertyAccess1()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class InvalidCollection
{
    public int Property { private get; set; }
}}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD003");

        // Assert that there is an error
        Assert.True(hasError, "The Property should generate an error.");
    }
    [Fact]
    public void ErrorOnInvalidPropertyAccess2()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class InvalidCollection
{
    public int Property { get; private set; }
}}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD003");

        // Assert that there is an error
        Assert.True(hasError, "The Property should generate an error.");
    }
    [Fact]
    public void IgnoreMember1()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class IgnoreMember
{
    [DataMemberIgnore]
    public int Property { get; set; }
}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Property shouldnt be considered when private.");
    }
    [Fact]
    public void IgnoreMember2()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreMember
{
    private int Property { get; set; }
}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Property shouldnt be considered when private.");
    }
}