using Microsoft.CodeAnalysis;

namespace Stride.Core.StrideDiagnostics.Tests;

public class DictionaryError
{
    [Fact]
    public void DataMemberIgnore_Attribute_On_Dictionary()
    {
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreCollection
{
    [DataMemberIgnore]
    internal System.Collections.Generic.Dictionary<int,string> Dictionary { private get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        var hasError = generatedDiagnostics.Any();
        Assert.True(!hasError, "The Property should be ignored with DataMemberIgnore.");
    }
    [Fact]
    public void Valid_DictionaryKeys_for_Primitives()
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
            var sourceCode = @$"
[DataContract]
public class IgnoreCollection
{{
    internal System.Collections.Generic.Dictionary<{primitiveType},string> Dictionary {{get; set; }}
}}";
            var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
            // Check if there are any diagnostics with the expected ID
            var hasError = generatedDiagnostics.Any();
            // Assert that there is an error
            Assert.False(hasError, $"The Dictionary Key for type {primitiveType} should be valid.");
        }
    }
    [Fact]
    public void Invalid_Dictionary_Key_for_objects()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
[DataContract]
public class IgnoreCollection
{
    internal System.Collections.Generic.Dictionary<System.Collections.Generic.List<int>,string> Dictionary {  get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(x => x.Id == ErrorCodes.DictionaryKey);

        // Assert that there is an error
        Assert.True(hasError, "The Dictionary Key should be invalid.");
    }
    [Fact]
    public void Invalid_Dictionary_Key_for_objects2()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
[DataContract]
public class IgnoreCollection
{
    internal System.Collections.Generic.Dictionary<object,string> Dictionary {  get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(x => x.Id == ErrorCodes.DictionaryKey);

        // Assert that there is an error
        Assert.True(hasError, "The Dictionary Key should be invalid.");
    }
    [Fact]
    public void Invalid_Dictionary_Access_On_private_Getter()
    {
        // Define the source code for the Class1 class with an invalid property
        var sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreCollection
{
    internal System.Collections.Generic.Dictionary<object,string> Dictionary { private get; set; }
}";
        var generatedDiagnostics = DiagnosticsHelper.GetDiagnostics(sourceCode);
        // Check if there are any diagnostics with the expected ID
        var hasError = generatedDiagnostics.Any(x => x.Id == ErrorCodes.CollectionAccess);

        // Assert that there is an error
        Assert.True(hasError, "The Dictionary Key should be invalid.");
    }
}
