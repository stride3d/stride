using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

using System.Runtime.Serialization;
using Stride.Core;

public class STRDIAG000_Test
{
    [Fact]
    public void Error_On_Attribute_Contradiction_On_Property()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class DoubleAnnotation
{
    [DataMemberIgnore]
    [DataMember]
    public int Value { get; set; }
}
";
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG000AttributeContradiction.DiagnosticId);
    }
    [Fact]
    public void Error_On_Attribute_Contradiction_On_Field()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class DoubleAnnotation
{
    [DataMemberIgnore]
    [DataMember]
    public int Value;
}
";
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG000AttributeContradiction.DiagnosticId);
    }
    // TODO missing test for DataMemberUpdatable as Stride.Engine isnt referenced
}
