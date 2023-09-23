using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.Tests;

public class PropertyError
{
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
        Assert.False(hasError, $"A DataMemberIgnore Property should never be considered when it has DataMemberIgnore:  {generatedDiagnostics.Select(x => x.Id)}.");
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
        Assert.False(hasError, $"A private Property should never be considered for Diagnostics when private: {generatedDiagnostics.Select(x => x.Id)}.");
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
        Assert.False(hasError, $"A private field should never be considered by a Diagnostics Error  {generatedDiagnostics.Select(x => x.Id)}.");
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
        Assert.False(hasError, $"A DataMemberIgnore field should never be considered by a Diagnostics Error: {generatedDiagnostics.Select(x => x.Id)}.");
    }
}
