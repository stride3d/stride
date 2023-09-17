using Microsoft.CodeAnalysis;
using StrideDiagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace StrideDiagnosticsTests;
public class DictionaryError
{
    [Fact]
    public void ValidDictionary()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class IgnoreCollection
{
    internal System.Collections.Generic.Dictionary<int,string> Dictionary { get; set; }
}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Property should be valid.");
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
    internal System.Collections.Generic.Dictionary<int,string> Dictionary { private get; set; }
}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any();

        // Assert that there is an error
        Assert.True(!hasError, "The Property should be ignored with DataMemberIgnore.");
    }
    [Fact]
    public void InvalidDictionaryKey1()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class IgnoreCollection
{
    internal System.Collections.Generic.Dictionary<System.Collections.Generic.List<int>,string> Dictionary {  get; set; }
}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(x => x.Id == ErrorCodes.DictionaryKey);

        // Assert that there is an error
        Assert.True(hasError, "The Dictionary Key should be invalid.");
    }
    [Fact]
    public void InvalidDictionaryKey2()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class IgnoreCollection
{
    internal System.Collections.Generic.Dictionary<object,string> Dictionary {  get; set; }
}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(x => x.Id == ErrorCodes.DictionaryKey);

        // Assert that there is an error
        Assert.True(hasError, "The Dictionary Key should be invalid.");
    }
    [Fact]
    public void InvalidDictionaryAccess()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
[DataContract]
public class IgnoreCollection
{
    internal System.Collections.Generic.Dictionary<object,string> Dictionary { private get; set; }
}";
        IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        bool hasError = generatedDiagnostics.Any(x => x.Id == ErrorCodes.CollectionAccess);

        // Assert that there is an error
        Assert.True(hasError, "The Dictionary Key should be invalid.");
    }
}
