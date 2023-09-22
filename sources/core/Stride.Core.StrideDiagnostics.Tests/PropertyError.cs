using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.Tests;

public class PropertyError
{
    [Fact]
    public void ErrorOnInvalidPropertyAccess_private_get()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class InvalidCollection
{
    public int Property { private get; set; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == ErrorCodes.InvalidPropertyAccess);

        // Assert that there is an error
        Assert.True(hasError, "The Property should generate an error.");
    }
    [Fact]
    public void ErrorOnInvalidPropertyAccess_private_set()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class InvalidProperty
{
    public int Property { get; private set; }
}}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any(diagnostic => diagnostic.Id == ErrorCodes.InvalidPropertyAccess);

        // Assert that there is an error
        Assert.True(hasError, "The Property should generate an error.");
    }

    [Fact]
    public void IgnoreMember_On_DataMemberIgnore_Properties()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreMember
{
    [DataMemberIgnore]
    public int Property { get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any();
        Assert.False(hasError, "The Property shouldnt be considered when private.");
    }
    [Fact]
    public void IgnoreMember_on_private_Properties()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreMember
{
    private int Property { get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any();
        Assert.False(hasError, "The Property shouldnt be considered when private.");
    }
    [Fact]
    public void IgnoreMember_on_private_fields()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreMember
{
    private int Property;
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any();
        Assert.False(hasError, "The field shouldnt be considered when private.");
    }
    [Fact]
    public void IgnoreMember_on_DataMemberIgnore_fields()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreMember
{
    [DataMemberIgnore]
    public int Property;
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any();
        Assert.False(hasError, "The Property shouldnt be considered when private.");
    }
}
