// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Annotations;

/// <summary>
/// Base class for a dynamic type attribute.
/// </summary>
public abstract class DynamicTypeAttributeBase : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicTypeAttributeBase"/> class.
    /// </summary>
    /// <param name="type">The type.</param>
    protected DynamicTypeAttributeBase(Type type)
    {
        TypeName = type.AssemblyQualifiedName ?? throw new ArgumentException("The type doesn't have an assembly-qualified name", nameof(type));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicTypeAttributeBase"/> class.
    /// </summary>
    /// <param name="typeName">The type.</param>
    protected DynamicTypeAttributeBase(string typeName)
    {
        TypeName = typeName;
    }

    /// <summary>
    /// Gets the name of the <see cref="DynamicTypeAttributeBase"/> type
    /// </summary>
    /// <value>The name of the serializable type.</value>
    public string TypeName { get; }
}
