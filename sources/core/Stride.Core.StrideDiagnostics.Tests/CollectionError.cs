using Microsoft.CodeAnalysis;
using Xunit;
namespace StrideDiagnosticsTests;

public class CollectionError
{
    [Fact]
    public void ErrorOnInvalidCollectionAccess()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class InvalidCollection
{
    public System.Collections.Generic.List<int> FancyList { private get; set; }
}}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(hasError, "The 'List' property should generate an error.");
    }
    [Fact]
    public void NoErrorOnCorrectCollectionAccess1()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class InvalidCollection
{
    public System.Collections.Generic.List<int> FancyList { get; set; }
}}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(!hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void NoErrorOnCorrectCollectionAccess2()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class InvalidCollection
{
    public System.Collections.Generic.List<int> FancyList { get; }
}}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(!hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void NoErrorOnCorrectCollectionAccess3()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class InvalidCollection
{
    internal System.Collections.Generic.List<int> FancyList { get; }
}}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(!hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void NoErrorOnCorrectCollectionAccess4()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class InvalidCollection
{
    internal System.Collections.Generic.List<int> FancyList { get; set; }
}}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(!hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void IgnoreMember1()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreCollection
{
    [DataMemberIgnore]
    internal System.Collections.Generic.List<int> FancyList { private get; set; }
}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Property should be ignored with DataMemberIgnore.");
    }
    [Fact]
    public void IgnoreMember2()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class IgnoreCollection
{
    private System.Collections.Generic.List<int> FancyList { private get; set; }
}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Property should be ignored with DataMemberIgnore.");
    }
}



