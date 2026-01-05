using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

/// <summary>
/// Tests for <see cref="STRDIAG000AttributeContradiction"/> analyzer.
/// Validates detection of contradictory DataMember and DataMemberIgnore attributes.
/// </summary>
public class STRDIAG000_Test
{
    [Fact]
    public async Task Error_On_Attribute_Contradiction_On_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMemberIgnore][DataMember]public int Value { get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG000AttributeContradiction.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_Attribute_Contradiction_On_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMemberIgnore][DataMember]public int Value;");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG000AttributeContradiction.DiagnosticId);
    }

    [Fact]
    public async Task Error_On_Reversed_Attribute_Order()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember][DataMemberIgnore]public int Value { get; set; }");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG000AttributeContradiction.DiagnosticId);
    }

    [Fact]
    public async Task NoErrorOn_Attribute_Contradiction_With_Updatable()
    {
        const string sourceCode = @"
using System;
using Stride.Core;
using Stride.Updater;
namespace Stride.Updater
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DataMemberUpdatableAttribute : Attribute
    {
    }
}

namespace Test
{
    [DataContract]
    public class TripleAnnotation
    {
        [DataMemberIgnore]
        [DataMemberUpdatable]
        [DataMember]
        public int Value;
    }
}
";
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_Only_DataMember()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMember]public int Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_Only_DataMemberIgnore()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMemberIgnore]public int Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task NoError_On_No_Attributes()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "public int Value { get; set; }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
}
