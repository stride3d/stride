using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.Tests;

public class CollectionError
{
    [Fact]
    public void Error_on_private_Getter_Access_on_Collection()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class InvalidCollection
{
    public System.Collections.Generic.List<int> FancyList { private get; set; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == ErrorCodes.InvalidCollectionAccess);
        Assert.True(hasError, "The 'List' property should generate an error. A private getter is not allowed.");
    }
    [Fact]
    public void Valid_Dictionary_Access_with_public_getter()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class InvalidCollection
{
    public System.Collections.Generic.List<int> FancyList { get; set; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == ErrorCodes.InvalidCollectionAccess);
        Assert.False(hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void Valid_Collection_Access_with_public_getter()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class InvalidCollection
{
    public System.Collections.Generic.List<int> FancyList { get; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == ErrorCodes.InvalidCollectionAccess);

        // Assert that there is an error
        Assert.False(hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void Valid_Collection_Access_with_internal_getter()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class InvalidCollection
{
    internal System.Collections.Generic.List<int> FancyList { get; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == ErrorCodes.InvalidCollectionAccess);

        Assert.False(hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void Valid_Collection_Access_with_public_getter_and_public_setter()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class InvalidCollection
{
    internal System.Collections.Generic.List<int> FancyList { get; set; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == "STRD002");

        // Assert that there is an error
        Assert.False(hasError, "The 'List' property Access should be valid.");
    }
    [Fact]
    public void DataMemberIgnore_Attribute_On_Collections()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreCollection
{
    [DataMemberIgnore]
    internal System.Collections.Generic.List<int> FancyList { private get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any();
        Assert.False(hasError, "The Property should be ignored with DataMemberIgnore.");
    }
    [Fact]
    public void Ignore_Private_Collection_Property()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreCollection
{
    private System.Collections.Generic.List<int> FancyList { private get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any();
        Assert.False(hasError, "The Property should be ignored with DataMemberIgnore.");
    }
}



