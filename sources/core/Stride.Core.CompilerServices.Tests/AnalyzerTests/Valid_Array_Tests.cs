using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class Valid_Properties_Tests
{
    [Fact]
    public void No_Error_On_GetOnly_public_Array()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidArray
{
    public int[] FancyList { get; }
}
";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_GetOnly_internal_Array()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidArray
{
    internal int[] FancyList { get; }
}
";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_Get_and_Set_public_Array()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidArray
{
    public int[] FancyList { get; set; }
}
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_Get_and_Set_internal_Array()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidArray
{
    internal int[] FancyList { get; set; }
}
@";

        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_private_Array()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidArray
{
    private int[] FancyList { private get; set; }
}
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_DataMemberIgnore_Array()
    {
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidArray
{
    [DataMemberIgnore]
    internal int[] FancyList { private get; set; }
}
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
}
