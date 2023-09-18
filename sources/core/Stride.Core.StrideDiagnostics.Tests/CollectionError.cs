using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.Tests;

public class CollectionError
{
    [Fact]
    public void ErrorOnInvalidCollectionAccess()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
[DataContract]
public class InvalidCollection
{
    public System.Collections.Generic.List<int> FancyList { private get; set; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(hasError, "The 'List' property should generate an error.");
    }
    [Fact]
    public void NoErrorOnCorrectCollectionAccess1()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
[DataContract]
public class InvalidCollection
{
    public System.Collections.Generic.List<int> FancyList { get; set; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(!hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void NoErrorOnCorrectCollectionAccess2()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
[DataContract]
public class InvalidCollection
{
    public System.Collections.Generic.List<int> FancyList { get; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(!hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void NoErrorOnCorrectCollectionAccess3()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
[DataContract]
public class InvalidCollection
{
    internal System.Collections.Generic.List<int> FancyList { get; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(!hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void NoErrorOnCorrectCollectionAccess4()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
[DataContract]
public class InvalidCollection
{
    internal System.Collections.Generic.List<int> FancyList { get; set; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.True(!hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void IgnoreMember1()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreCollection
{
    [DataMemberIgnore]
    internal System.Collections.Generic.List<int> FancyList { private get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Property should be ignored with DataMemberIgnore.");
    }
    [Fact]
    public void IgnoreMember2()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
[DataContract]
public class IgnoreCollection
{
    private System.Collections.Generic.List<int> FancyList { private get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Property should be ignored with DataMemberIgnore.");
    }
}



