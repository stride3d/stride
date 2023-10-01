using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;
public class ValidFields
{
    [Fact]
    public void No_Error_On_private_Field()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidField
{
    private int FancyList;
}
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
    [Fact]
    public void No_Error_On_public_Field()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidField
{
    public int FancyList;
}
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
    [Fact]
    public void No_Error_On_readonly_Field()
    {
        string sourceCode = @"
using Stride.Core;
using System.Collections.Generic;
[DataContract]
public class ValidField
{
    public readonly List<int> FancyList;
}
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
}
