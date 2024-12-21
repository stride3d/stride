using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG008_Test
{
    [Fact]
    public void Error_On_DataMembered_Delegate_Property()
    {
        string sourceCode = @"
using Stride.Core;
using System;
[DataContract]
public unsafe struct B
{
    [DataMember]
    public fixed byte T[12];
}
";
        TestHelper.ExpectDiagnosticsError(sourceCode, STRDIAG008FixedFieldInStructs.DiagnosticId);
    }
}
