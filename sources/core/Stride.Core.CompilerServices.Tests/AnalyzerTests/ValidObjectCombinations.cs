using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class ValidObjectCombinations
{
    private string PublicFormat(string access) => string.Format(ClassTemplates.PublicClassTemplateNoDatamember, access, "object");
    private string PublicFormatWithDataMember(string access) => string.Format(ClassTemplates.PublicClassTemplateDataMember, access,"object");
    private string InternalFormat(string access) => string.Format(ClassTemplates.InternalClassTemplate, access,"object");
    [Fact]
    public void No_Error_On_Public_Properties_No_DataMember()
    {
        var combinations = new string[] {
            "get;set;",
            "get => null;set {{ }}",
            "get;",
            "get;private set;",
            "get;internal set;",
            "get;protected set;",
            "get;private protected set;",
            "get;internal protected set;"
        };
        foreach (var combination in combinations)
        {
            string sourceCode = PublicFormat(combination);
            TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
        }
    }
    [Fact]
    public void No_Error_On_Public_Properties_DataMember()
    {
        var combinations = new string[] {
            "get;set;",
            "get => null;set {{ }}",
            "get;",
            "get => null;",
            "get;private set;",
            "get;internal set;",
            "get;protected set;",
            "get;private protected set;",
            "get;internal protected set;"
        };
        foreach (var combination in combinations)
        {
            string sourceCode = PublicFormatWithDataMember(combination);
            TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
        }
    }
    [Fact]
    public void No_Error_On_internal_Properties()
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
            string sourceCode = InternalFormat(combination);
            TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
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
            string sourceCode = string.Format(ClassTemplates.AccessorTemplate,combination,"object");
            TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
        }
    }
}
