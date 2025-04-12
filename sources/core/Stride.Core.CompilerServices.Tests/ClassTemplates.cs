namespace Stride.Core.CompilerServices.Tests;

internal static class ClassTemplates
{
    public const string PublicClassTemplateNoDatamember = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection
{{
    public {1} X {{ {0} }}
}}
";

    public const string PublicClassTemplateDataMember = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection
{{
    [Stride.Core.DataMember]
    public {1} X {{ {0} }}
}}
";

    public const string InternalClassTemplate = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection
{{
    [Stride.Core.DataMember]
    internal {1} X {{ {0} }}
}}
";

    public const string BasicClassTemplate = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection
{{
    {0}
}}
";

    public const string AccessorTemplate = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection
{{
    
    {0} {1} X {{ get; set; }}
}}
";
    public const string InheritedDataContract = @"
using Stride.Core;
using System;
[DataContract(Inherited = true)]
public class Base {{ }}

public class Inherited : Base
{{
    {0}
}}
";

    public const string PrimaryConstructorTemplate = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection(int x)
{{
    {0}
}}
";
    public const string DataContractArgumentsTemplate = @"
[DataContract(Inherited = true,DefaultMemberMode = DataMemberMode.Assign)]
public struct ValidCollection
{{
}}
[DataContract(DefaultMemberMode = DataMemberMode.Assign,Inherited = true)]
public struct ValidCollection2
{{
    {0}
}}
";
}
