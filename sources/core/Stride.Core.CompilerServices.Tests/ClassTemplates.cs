namespace Stride.Core.CompilerServices.Tests;
internal class ClassTemplates
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

}
