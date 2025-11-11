using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class ValidModifiersOnMutableMemberTests
{
    private static string PublicFormat(string access) => string.Format(ClassTemplates.PublicClassTemplateNoDatamember, access, "object");
    private static string PublicFormatWithDataMember(string access) => string.Format(ClassTemplates.PublicClassTemplateDataMember, access, "object");
    private static string InternalFormatWithDataMember(string access) => string.Format(ClassTemplates.InternalClassTemplate, access, "object");

    /// <summary>
    /// The Serializers serialize some Properties per default.
    /// These don't require a [DataMember] Attribute
    /// 
    /// The Analyzers shouldn't ever throw on these Combinations
    /// </summary>
    [Fact]
    public async Task No_Error_On_Public_Properties_No_DataMember()
    {
        var combinations = new string[] {
            "get;set;",
            "get => null;set {{ }}",
            "get;",
            "get;private set;",
            // this will be treated as Content Mode
            "get;internal set;",
            "get;protected set;",
            "get;private protected set;",
            "get;internal protected set;"
        };
        foreach (var combination in combinations)
        {
            string sourceCode = PublicFormat(combination);
            await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
        }
    }
    /// <summary>
    /// The Serializers serialize some Properties per default.
    /// These don't require a [DataMember] Attribute
    /// 
    /// The Diagnostics shouldn't ever throw on these Combinations
    /// The Major differences are compared to no [DataMember] is that get only Properties without a backingfield are serialized now
    /// Also internal set will be treated as a valid set for assign mode
    /// </summary>
    [Fact]
    public async Task No_Error_On_Public_Properties_DataMember()
    {
        var combinations = new string[] {
            "get;set;",
            "get => null;set {{ }}",
            "internal get => null;set {{ }}",
            "internal get;set;",
            "get;",
            "get => null;",
            "get;private set;",
            // this will be treated as Assign Mode, if not tagged as Content Mode
            "get;internal set;",
            "get;protected set;",
            "get;private protected set;",
            "get;internal protected set;"
        };
        foreach (var combination in combinations)
        {
            string sourceCode = PublicFormatWithDataMember(combination);
            await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
        }
    }

    /// <summary>
    /// internal properties won't get serialized per default
    /// It's always necessary to tag them with [DataMember]
    /// </summary>
    [Fact]
    public async Task No_Error_On_internal_Properties()
    {
        var combinations = new string[] {
            "get;set;",
            "get => null;set {{ }}",
            "get;",
            "get => null;",
            "get;private set;",
            "get;protected set;",
            "get;private protected set;",
            "get;internal protected set;"
        };
        foreach (var combination in combinations)
        {
            string sourceCode = InternalFormatWithDataMember(combination);
            await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
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
            string sourceCode = string.Format(ClassTemplates.AccessorTemplate, combination, "object");
            await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
        }
    }
}
