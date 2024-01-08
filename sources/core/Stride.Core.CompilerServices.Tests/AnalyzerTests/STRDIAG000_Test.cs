using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG000_Test
{
    [Fact]
    public void Error_On_Attribute_Contradiction_On_Property()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMemberIgnore][DataMember]public int Value { get; set; }");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG000AttributeContradiction.DiagnosticId);
    }

    [Fact]
    public void Error_On_Attribute_Contradiction_On_Field()
    {
        string sourceCode = string.Format(ClassTemplates.BasicClassTemplate, "[DataMemberIgnore][DataMember]public int Value;");
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG000AttributeContradiction.DiagnosticId);
    }

    [Fact]
    public void NoErrorOn_Attribute_Contradiction_With_Updatable()
    {
        string sourceCode = @"
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
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
}
