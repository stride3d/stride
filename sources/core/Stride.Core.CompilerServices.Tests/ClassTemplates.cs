namespace Stride.Core.CompilerServices.Tests;

/// <summary>
/// Provides reusable code templates for testing analyzers.
/// </summary>
internal static class ClassTemplates
{
    /// <summary>
    /// Template for a public class with a property without [DataMember] attribute.
    /// Format: {0} = property accessor pattern (e.g., "get; set;"), {1} = property type
    /// </summary>
    public const string PublicClassTemplateNoDatamember = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection
{{
    public {1} X {{ {0} }}
}}
";

    /// <summary>
    /// Template for a public class with a property with [DataMember] attribute.
    /// Format: {0} = property accessor pattern (e.g., "get; set;"), {1} = property type
    /// </summary>
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

    /// <summary>
    /// Template for a public class with an internal property with [DataMember] attribute.
    /// Format: {0} = property accessor pattern (e.g., "get; set;"), {1} = property type
    /// </summary>
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

    /// <summary>
    /// Template for a basic public class with custom member content.
    /// Format: {0} = member declarations
    /// </summary>
    public const string BasicClassTemplate = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection
{{
    {0}
}}
";

    /// <summary>
    /// Template for testing different accessor combinations.
    /// Format: {0} = access modifier, {1} = member type
    /// </summary>
    public const string AccessorTemplate = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection
{{
    
    {0} {1} X {{ get; set; }}
}}
";

    /// <summary>
    /// Template for testing inherited DataContract scenarios.
    /// Format: {0} = derived class member declarations
    /// </summary>
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

    /// <summary>
    /// Template for testing primary constructor scenarios.
    /// Format: {0} = class member declarations
    /// </summary>
    public const string PrimaryConstructorTemplate = @"
using Stride.Core;
using System;
[DataContract]
public class ValidCollection(int x)
{{
    {0}
}}
";

    /// <summary>
    /// Template for testing DataContract with various arguments.
    /// Format: {0} = additional member declarations for ValidCollection2
    /// </summary>
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
