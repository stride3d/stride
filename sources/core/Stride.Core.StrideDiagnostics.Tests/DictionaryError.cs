using Microsoft.CodeAnalysis;
using StrideDiagnostics;

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
    public void ValidDictionaryKeysForPrimitives()
    {
        var primitiveTypes = new HashSet<string>
        {
            "bool",
            "Boolean",
            "byte",
            "Byte",
            "sbyte",
            "SByte",
            "char",
            "Char",
            "decimal",
            "Decimal",
            "double",
            "Double",
            "float",
            "Single", // Note that Single is the non-aliased name for float.
            "int",
            "Int32", // Note that Int32 is the non-aliased name for int.
            "uint",
            "UInt32", // Note that UInt32 is the non-aliased name for uint.
            "long",
            "Int64", // Note that Int64 is the non-aliased name for long.
            "ulong",
            "UInt64", // Note that UInt64 is the non-aliased name for ulong.
            "short",
            "Int16", // Note that Int16 is the non-aliased name for short.
            "ushort",
            "UInt16", // Note that UInt16 is the non-aliased name for ushort.
            "string",
            "String"
        };
        foreach (var primitiveType in primitiveTypes)
        {
            // Define the source code for the Class1 class with an invalid property
            string sourceCode = @$"
[DataContract]
public class IgnoreCollection
{{
    internal System.Collections.Generic.Dictionary<{primitiveType},string> Dictionary {{get; set; }}
}}";
            IEnumerable<Diagnostic> generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
            // Check if there are any diagnostics with the expected ID
            bool hasError = generatedDiagnostics.Any();
            // Assert that there is an error
            Assert.False(hasError, $"The Dictionary Key for type {primitiveType} should be valid.");
        }
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
