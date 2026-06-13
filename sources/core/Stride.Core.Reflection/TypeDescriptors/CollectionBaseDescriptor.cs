// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection;

public abstract class CollectionBaseDescriptor : ObjectDescriptor
{
    protected CollectionBaseDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention) : base(factory, type, emitDefaultValues, namingConvention)
    {
    }

    /// <summary>
    /// Returns the value at the key specified in the collection.
    /// </summary>
    /// <remarks> The key would be the integer index for list-like collections </remarks>
    public abstract object? GetValue(object collection, object key);

    /// <summary>
    /// Sets the value at key to value in the collection provided.
    /// </summary>
    /// <remarks> The key would be the integer index for list-like collections </remarks>
    public abstract void SetValue(object collection, object key, object? value);

    /// <summary>
    /// The type of value returned through <see cref="GetValue"/>
    /// </summary>
    public abstract Type ValueType { get; }

    public abstract IEnumerable<object> EnumerateKeys(object collection);

    /// <summary>
    /// Validates whether the type of this key would be valid
    /// </summary>
    /// <remarks>
    /// For ints it also checks if its value is above 0
    /// </remarks>
    public abstract bool IsKeyValid(object? key);

    /// <summary>
    /// Checks whether there is a value for this key
    /// </summary>
    public abstract bool ContainsKey(object collection, object? key);
}
