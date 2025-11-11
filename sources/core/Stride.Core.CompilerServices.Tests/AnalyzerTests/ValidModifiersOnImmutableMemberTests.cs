using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class ValidModifiersOnImmutableMemberTests
{
    private static string PublicFormat(string access, string type) => string.Format(ClassTemplates.PublicClassTemplateNoDatamember, access, type);
    private static string PublicFormatWithDataMember(string access, string type) => string.Format(ClassTemplates.PublicClassTemplateDataMember, access, type);
    private static string InternalFormat(string access, string type) => string.Format(ClassTemplates.InternalClassTemplate, access, type);
    private readonly string[] Types =
    [
        "string",
        "int",
        "float",
        "double",
    ];
    /// <summary>
    /// Compared to public Properties with a mutable reference type, Content mode is never valid for Reference Types
    /// So only Assign Mode combinations are per default serialized for ImmutableMembers
    /// </summary>
    [Fact]
    public async Task No_Error_On_Public_Properties_No_DataMember()
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
                await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
            }
        }
    }
    /// <summary>
    /// With [DataMember] on ImmutableTypes combinations with internal get/set get also valid
    /// as the set and get treated then as Visible to the serializers
    /// </summary>
    [Fact]
    public async Task No_Error_On_Public_Properties_DataMember()
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
                await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
            }
        }
    }
    /// <summary>
    /// internal properties only get serialized with [DataMember] Attribute.
    /// Only Assign Mode combinations are allowed.
    /// </summary>
    [Fact]
    public async Task No_Error_On_internal_Properties()
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
                await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
            }
        }
    }

    /// <summary>
    /// Properties that the Serializers can't access won't get serialized.
    /// As long as there is no [DataMember] Attribute on them, the Analyzers should never throw on them
    /// </summary>
    [Fact]
    public async Task No_Error_On_Inaccessible_properties()
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
                await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
            }
        }
    }
}
