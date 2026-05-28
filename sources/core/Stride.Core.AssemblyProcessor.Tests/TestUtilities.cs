// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.AssemblyProcessor.Tests;

public class TestUtilities
{
    [Fact]
    public void TestBuildValidClassNameSimple()
    {
        Assert.Equal("MyClass", Utilities.BuildValidClassName("MyClass"));
        Assert.Equal("my_class", Utilities.BuildValidClassName("my_class"));
    }

    [Fact]
    public void TestBuildValidClassNameWithInvalidCharacters()
    {
        Assert.Equal("My_Class", Utilities.BuildValidClassName("My Class"));
        Assert.Equal("My_Class", Utilities.BuildValidClassName("My-Class"));
        Assert.Equal("My_Class", Utilities.BuildValidClassName("My+Class"));
        Assert.Equal("My_Class_Name", Utilities.BuildValidClassName("My@Class#Name"));
        Assert.Equal("Test___Data", Utilities.BuildValidClassName("Test!@#Data"));
    }

    [Fact]
    public void TestBuildValidClassNameStartsWithNumber()
    {
        Assert.Equal("_123Class", Utilities.BuildValidClassName("123Class"));
        Assert.Equal("_7Days", Utilities.BuildValidClassName("7Days"));
    }

    [Fact]
    public void TestBuildValidClassNameReservedKeywords()
    {
        Assert.Equal("class_", Utilities.BuildValidClassName("class"));
        Assert.Equal("int_", Utilities.BuildValidClassName("int"));
        Assert.Equal("string_", Utilities.BuildValidClassName("string"));
        Assert.Equal("namespace_", Utilities.BuildValidClassName("namespace"));
        Assert.Equal("public_", Utilities.BuildValidClassName("public"));
        Assert.Equal("private_", Utilities.BuildValidClassName("private"));
        Assert.Equal("void_", Utilities.BuildValidClassName("void"));
    }

    [Fact]
    public void TestBuildValidClassNameWithCustomReplacementCharacter()
    {
        Assert.Equal("My@Class", Utilities.BuildValidClassName("My Class", '@'));
        Assert.Equal("Test$Name", Utilities.BuildValidClassName("Test-Name", '$'));
    }

    [Fact]
    public void TestBuildValidClassNameWithAdditionalReservedWords()
    {
        var additionalReservedWords = new[] { "CustomReserved", "AnotherReserved" };

        Assert.Equal("CustomReserved_", Utilities.BuildValidClassName("CustomReserved", additionalReservedWords));
        Assert.Equal("AnotherReserved_", Utilities.BuildValidClassName("AnotherReserved", additionalReservedWords));
        Assert.Equal("NormalName", Utilities.BuildValidClassName("NormalName", additionalReservedWords));
    }

    [Fact]
    public void TestBuildValidClassNameSpecialCharacters()
    {
        Assert.Equal("a_b_c", Utilities.BuildValidClassName("a;b,c"));
        Assert.Equal("test_query", Utilities.BuildValidClassName("test?query"));
        Assert.Equal("angle_brackets_", Utilities.BuildValidClassName("angle<brackets>"));
        Assert.Equal("quotes_", Utilities.BuildValidClassName("quotes\""));
    }

    [Fact]
    public void TestBuildValidNamespaceNameSimple()
    {
        Assert.Equal("MyNamespace", Utilities.BuildValidNamespaceName("MyNamespace"));
        Assert.Equal("My.Namespace", Utilities.BuildValidNamespaceName("My.Namespace"));
    }

    [Fact]
    public void TestBuildValidNamespaceNameWithInvalidCharacters()
    {
        Assert.Equal("My_Namespace", Utilities.BuildValidNamespaceName("My Namespace"));
        Assert.Equal("My_Namespace", Utilities.BuildValidNamespaceName("My-Namespace"));
    }

    [Fact]
    public void TestBuildValidNamespaceNameStartsWithNumber()
    {
        Assert.Equal("_123Namespace", Utilities.BuildValidNamespaceName("123Namespace"));
    }

    [Fact]
    public void TestBuildValidNamespaceNameReservedKeywords()
    {
        Assert.Equal("class_", Utilities.BuildValidNamespaceName("class"));
        Assert.Equal("namespace_", Utilities.BuildValidNamespaceName("namespace"));
    }

    [Fact]
    public void TestBuildValidNamespaceNameDotFollowedByNumber()
    {
        // Dots followed by numbers should be replaced
        Assert.Equal("Version_2_0", Utilities.BuildValidNamespaceName("Version.2.0"));
    }

    [Fact]
    public void TestBuildValidNamespaceNameWithCustomReplacementCharacter()
    {
        Assert.Equal("My@Namespace", Utilities.BuildValidNamespaceName("My Namespace", '@'));
    }

    [Fact]
    public void TestBuildValidNamespaceNameWithAdditionalReservedWords()
    {
        var additionalReservedWords = new[] { "CustomReserved" };
        Assert.Equal("CustomReserved_", Utilities.BuildValidNamespaceName("CustomReserved", additionalReservedWords));
    }

    [Fact]
    public void TestBuildValidProjectNameSimple()
    {
        Assert.Equal("MyProject", Utilities.BuildValidProjectName("MyProject"));
        Assert.Equal("My.Project", Utilities.BuildValidProjectName("My.Project"));
    }

    [Fact]
    public void TestBuildValidProjectNameWithInvalidCharacters()
    {
        Assert.Equal("My_Project", Utilities.BuildValidProjectName("My=Project"));
        Assert.Equal("Path_To_Project", Utilities.BuildValidProjectName("Path/To/Project"));
        Assert.Equal("Query_String", Utilities.BuildValidProjectName("Query?String"));
        Assert.Equal("Colon_Name", Utilities.BuildValidProjectName("Colon:Name"));
        Assert.Equal("Ampersand_Name", Utilities.BuildValidProjectName("Ampersand&Name"));
        Assert.Equal("Asterisk_Name", Utilities.BuildValidProjectName("Asterisk*Name"));
        Assert.Equal("Less_Greater_", Utilities.BuildValidProjectName("Less<Greater>"));
        Assert.Equal("Pipe_Name", Utilities.BuildValidProjectName("Pipe|Name"));
        Assert.Equal("Hash_Name", Utilities.BuildValidProjectName("Hash#Name"));
        Assert.Equal("Percent_Name", Utilities.BuildValidProjectName("Percent%Name"));
        Assert.Equal("Quote_", Utilities.BuildValidProjectName("Quote\""));
    }

    [Fact]
    public void TestBuildValidProjectNameWithCustomReplacementCharacter()
    {
        Assert.Equal("My-Project", Utilities.BuildValidProjectName("My/Project", '-'));
    }

    [Fact]
    public void TestBuildValidFileNameSimple()
    {
        Assert.Equal("MyFile", Utilities.BuildValidFileName("MyFile"));
        Assert.Equal("My File", Utilities.BuildValidFileName("My File")); // Spaces are allowed in filenames
    }

    [Fact]
    public void TestBuildValidFileNameWithInvalidCharacters()
    {
        Assert.Equal("My_File", Utilities.BuildValidFileName("My=File"));
        Assert.Equal("Path_To_File", Utilities.BuildValidFileName("Path/To/File"));
        Assert.Equal("Query_String", Utilities.BuildValidFileName("Query?String"));
        Assert.Equal("Colon_Name", Utilities.BuildValidFileName("Colon:Name"));
        Assert.Equal("Ampersand_Name", Utilities.BuildValidFileName("Ampersand&Name"));
        Assert.Equal("Exclamation_Name", Utilities.BuildValidFileName("Exclamation!Name"));
        Assert.Equal("Dot__", Utilities.BuildValidFileName("Dot.*")); // Both . and * are invalid
        Assert.Equal("Less_Greater_", Utilities.BuildValidFileName("Less<Greater>"));
        Assert.Equal("Pipe_Name", Utilities.BuildValidFileName("Pipe|Name"));
        Assert.Equal("Hash_Name", Utilities.BuildValidFileName("Hash#Name"));
        Assert.Equal("Percent_Name", Utilities.BuildValidFileName("Percent%Name"));
        Assert.Equal("Quote_", Utilities.BuildValidFileName("Quote\""));
    }

    [Fact]
    public void TestBuildValidFileNameWithCustomReplacementCharacter()
    {
        Assert.Equal("My-File", Utilities.BuildValidFileName("My/File", '-'));
    }

    [Fact]
    public void TestBuildValidClassNameEdgeCases()
    {
        // Underscore is valid, should not be changed
        Assert.Equal("_", Utilities.BuildValidClassName("_"));

        // Multiple consecutive invalid characters
        Assert.Equal("Test____Name", Utilities.BuildValidClassName("Test!@#$Name"));

        // All reserved characters (19 characters in the input string)
        Assert.Equal("___________________", Utilities.BuildValidClassName(" -;',+*|!`~@#$%^&?"));
    }

    [Fact]
    public void TestBuildValidNamespaceNamePreservesDots()
    {
        // Dots should be preserved in namespace names (except when followed by numbers)
        Assert.Equal("System.Collections.Generic", Utilities.BuildValidNamespaceName("System.Collections.Generic"));
    }

    [Fact]
    public void TestBuildValidClassNameCommonPatterns()
    {
        // Common naming patterns
        Assert.Equal("IMyInterface", Utilities.BuildValidClassName("IMyInterface"));
        Assert.Equal("_privateField", Utilities.BuildValidClassName("_privateField"));
        Assert.Equal("MyClass123", Utilities.BuildValidClassName("MyClass123"));
        Assert.Equal("CONSTANT_VALUE", Utilities.BuildValidClassName("CONSTANT_VALUE"));
    }
}
