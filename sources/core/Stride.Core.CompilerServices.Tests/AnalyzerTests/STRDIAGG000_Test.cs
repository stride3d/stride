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

public class STRDIAGG000_Test
{
    [Fact]
    public void Error_On_Attribute_Contradiction_On_Property()
    {
        // Define the source code for the Class1 class with an invalid property
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
        // Define the source code for the Class1 class with an invalid property
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
}
