using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class ValidImmutableCombinations
{
    private string PublicFormat(string access, string type) => string.Format(ClassTemplates.PublicClassTemplateNoDatamember, access, type);
    private string PublicFormatWithDataMember(string access, string type) => string.Format(ClassTemplates.PublicClassTemplateDataMember, access, type);
    private string InternalFormat(string access, string type) => string.Format(ClassTemplates.InternalClassTemplate, access, type);
    private string[] Types = new string[]
    {
        "string",
        "int",
        "float",
        "double",
        "String"
    };

    [Fact]
    public void No_Error_On_Public_Properties_No_DataMember()
    {
        var combinations = new string[] {
            "get;set;",
            "get => null;set { }",
        };
        foreach (var combination in combinations)
        {
            foreach (var type in Types)
            {
                string sourceCode = PublicFormat(combination, type);
                TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
            }
        }
    }

    [Fact]
    public void No_Error_On_Public_Properties_DataMember()
    {
        var combinations = new string[] {
            "get;set;",
            "get => null;set { }",
            "get;internal set;",
            "internal get;set;",
            "get;internal protected set;"
        };
        foreach (var combination in combinations)
        {
            foreach (var type in Types)
            {
                string sourceCode = PublicFormatWithDataMember(combination, type);
                TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
            }
        }
    }

    [Fact]
    public void No_Error_On_internal_Properties()
    {
        var combinations = new string[] {
            "get;set;",
            "get => default();set { }",
            "get;internal protected set;"
        };
        foreach (var combination in combinations)
        {
            foreach (var type in Types)
            {
                string sourceCode = InternalFormat(combination, type);
                TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
            }
        }
    }

    [Fact]
    public void No_Error_On_Inaccessible_properties()
    {
        var combinations = new string[] {
            "private",
            "protected",
            "private protected"
        };
        foreach (var combination in combinations)
        {
            foreach (var type in Types)
            {
                string sourceCode = string.Format(ClassTemplates.AccessorTemplate, combination, type);
                TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
            }
        }
    }
}

