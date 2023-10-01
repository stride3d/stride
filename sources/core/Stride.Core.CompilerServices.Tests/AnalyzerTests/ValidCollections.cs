using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class ValidCollections
{
    [Fact]
    public void No_Error_On_GetOnly_public_Collection()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidCollection
{
    public System.Collections.Generic.List<int> FancyList { get; }
}
";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_GetOnly_internal_Collection()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidCollection
{
    internal System.Collections.Generic.List<int> FancyList { get; }
}
";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_Get_and_Set_public_Collection()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidCollection
{
    public System.Collections.Generic.List<int> FancyList { get; set; }
}
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_Get_and_Set_internal_Collection()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidCollection
{
    internal System.Collections.Generic.List<int> FancyList { get; set; }
}
@";

        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_GetOnly_public_ICollection_generic()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class ValidCollection
{
    public System.Collections.Generic.ICollection<int> FancyList { get; }
};
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_private_Collection()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreCollection
{
    private System.Collections.Generic.List<int> FancyList { private get; set; }
}
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }

    [Fact]
    public void No_Error_On_DataMemberIgnore_Collection()
    {
        // Define the source code for the Class1 class with an invalid property
        string sourceCode = @"
using Stride.Core;
[DataContract]
public class IgnoreCollection
{
    [DataMemberIgnore]
    internal System.Collections.Generic.List<int> FancyList { private get; set; }
}
@";
        TestHelper.ExpectNoDiagnosticsErrors(sourceCode);
    }
}
