// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core;

/// <summary>
/// Represents a container that can hold properties, lightweight to embed (lazy initialized).
/// </summary>
/// <remarks>
/// Tag properties system purpose is to allow binding of properties that aren't logically supposed to be
/// in a general class (probably because the property exists only in a higher level part of the engine).
/// A typical example includes source mesh, collision data and various bouding volumes for a Geometry object:
/// including them directly in the low-level Geometry class would be a terrible design decision !
/// And the other well known solution, which consist of maintaining a Dictionary of object to properties
/// isn't really nice either (especially with non-deterministic object destruction, it's hard to clean it up, would require lot of events).
/// As a result, a specific system has been implemented.
/// A class that could hold such tag properties should have an instance of <see cref="PropertyContainer"/> as a mutable field member.
/// An cool feature of this system is that if a property doesn't exist, it could be generated during first access from a delegate or come from a default value.
/// </remarks>
[DataContract]
[DataSerializer(typeof(DictionaryAllSerializer<PropertyContainer, PropertyKey, object>))]
public struct PropertyContainer : IDictionary<PropertyKey, object>, IReadOnlyDictionary<PropertyKey, object>
{
    private static readonly Dictionary<Type, List<PropertyKey>> AccessorProperties = [];
    private Dictionary<PropertyKey, object>? properties;

    /// <summary>
    /// Property changed delegate.
    /// </summary>
    /// <param name="propertyContainer">The property container.</param>
    /// <param name="propertyKey">The property key.</param>
    /// <param name="newValue">The property new value.</param>
    /// <param name="oldValue">The property old value.</param>
    public delegate void PropertyUpdatedDelegate(ref PropertyContainer propertyContainer, PropertyKey propertyKey, object newValue, object? oldValue);

    /// <summary>
    /// Occurs when a property is modified.
    /// </summary>
    public event PropertyUpdatedDelegate? PropertyUpdated;

    public PropertyContainer(object owner)
    {
        properties = null;
        PropertyUpdated = null;
        Owner = owner;
    }

    [DataMemberIgnore]
    public object Owner { get; }

    /// <summary>
    /// Gets the key-properties value pairs in this instance.
    /// </summary>
    public IEnumerator<KeyValuePair<PropertyKey, object>> GetEnumerator()
    {
        if (properties != null)
        {
            foreach (var property in properties)
            {
                yield return new KeyValuePair<PropertyKey, object>(property.Key, property.Key.IsValueType ? ((ValueHolder)property.Value).ObjectValue : property.Value);
            }
        }

        if (Owner != null)
        {
            var currentType = Owner.GetType();
            while (currentType != null)
            {
                if (AccessorProperties.TryGetValue(currentType, out var typeAccessorProperties))
                {
                    foreach (var accessorProperty in typeAccessorProperties)
                    {
                        yield return new KeyValuePair<PropertyKey, object>(accessorProperty, accessorProperty.AccessorMetadata.GetValue(ref this));
                    }
                }

                currentType = currentType.GetTypeInfo().BaseType;
            }
        }
    }

    public readonly void Clear()
    {
        properties?.Clear();
    }

    /// <summary>
    /// Gets the number of properties stored in this container.
    /// </summary>
    /// <value>The count of properties.</value>
    public int Count
    {
        get
        {
            // TODO: improve this.
            var count = 0;
            using (var enumerator = GetEnumerator())
            {
                while (enumerator.MoveNext())
                    ++count;
            }
            return count;
        }
    }

    public bool IsReadOnly => false;

    /// <summary>
    /// Adds the specified key-value pair.
    /// </summary>
    /// <typeparam name="T">Type of the property key</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Add<T>(PropertyKey<T> key, T value)
    {
        SetObject(key, value, true);
    }

    /// <summary>
    /// Determines whether the specified instance contains this key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>
    ///   <c>true</c> if the specified instance contains this key; otherwise, <c>false</c>.
    /// </returns>
    public readonly bool ContainsKey(PropertyKey key)
    {
        // If it's a key with AccessorMetadata, check if it has been registered to this type
        // Not very efficient... hopefully it should be rarely used. If not, it should be quite easy to optimize.
        if (key.AccessorMetadata != null && Owner != null)
        {
            var currentType = Owner.GetType();
            while (currentType != null)
            {
                if (AccessorProperties.TryGetValue(currentType, out var typeAccessorProperties))
                {
                    foreach (var accessorProperty in typeAccessorProperties)
                    {
                        if (accessorProperty == key)
                            return true;
                    }
                }

                currentType = currentType.GetTypeInfo().BaseType;
            }

            return false;
        }

        return properties?.ContainsKey(key) == true;
    }

    void IDictionary<PropertyKey, object>.Add(PropertyKey key, object value)
    {
        SetObject(key, value, true);
    }

    public bool Remove(PropertyKey propertyKey)
    {
        var removed = false;

        var previousValue = Get(propertyKey);
        if (PropertyUpdated != null || propertyKey.PropertyUpdateCallback != null)
        {
            if (properties != null)
                removed = properties.Remove(propertyKey);
            var tagValue = Get(propertyKey);

            if (!ArePropertyValuesEqual(propertyKey, tagValue, previousValue))
            {
                propertyKey.PropertyUpdateCallback?.Invoke(ref this, propertyKey, tagValue, previousValue);
                PropertyUpdated?.Invoke(ref this, propertyKey, tagValue, previousValue);
            }
        }
        else
        {
            if (properties != null)
                removed = properties.Remove(propertyKey);
        }

        propertyKey.ObjectInvalidationMetadata?.Invalidate(Owner, propertyKey, previousValue);

        return removed;
    }

    public object this[PropertyKey key]
    {
        get
        {
            return Get(key);
        }
        set
        {
            SetObject(key, value);
        }
    }

    public readonly ICollection<PropertyKey> Keys
    {
        get
        {
            return this.Select(x => x.Key).ToList();
        }
    }

    public readonly ICollection<object> Values
    {
        get
        {
            return this.Select(x => x.Value).ToList();
        }
    }

    /// <summary>
    /// Copies properties from this instance to a container.
    /// </summary>
    /// <param name="destination">The destination.</param>
    public void CopyTo(ref PropertyContainer destination)
    {
        foreach (var keyValuePair in this)
        {
            destination.SetObject(keyValuePair.Key, keyValuePair.Value);
        }
    }

    /// <summary>
    /// Gets the specified tag value.
    /// </summary>
    /// <param name="propertyKey">The tag property.</param>
    /// <returns>Value of the tag property</returns>
    public object Get(PropertyKey propertyKey)
    {
        return Get(propertyKey, false);
    }

    private object? Get(PropertyKey propertyKey, bool forceNotToKeep)
    {
        // First, check if there is an accessor
        if (propertyKey.AccessorMetadata != null)
        {
            return propertyKey.AccessorMetadata.GetValue(ref this);
        }


        // Get bound value, if any.
        if (properties != null && properties.TryGetValue(propertyKey, out var value))
        {
            if (propertyKey.IsValueType)
                value = ((ValueHolder)value).ObjectValue;
            return value;
        }

        if (propertyKey.DefaultValueMetadata != null)
        {
            // Get default value.
            var defaultValue = propertyKey.DefaultValueMetadata.GetDefaultValue(ref this);

            // Check if value should be kept.
            if (propertyKey.DefaultValueMetadata.KeepValue && !forceNotToKeep)
            {
                // Register it.
                SetObject(propertyKey, defaultValue);
            }
            return defaultValue;
        }

        return null;
    }

    /// <summary>
    /// Gets the value of a property key or throw an error if the value was not found
    /// </summary>
    /// <typeparam name="T">Type of the property key</typeparam>
    /// <param name="propertyKey">The property key.</param>
    /// <returns>The value associated with this property key.</returns>
    /// <exception cref="ArgumentNullException">propertyKey</exception>
    /// <exception cref="ArgumentException">Unable to retrieve value for [{0}].ToFormat(propertyKey)</exception>
    public T GetSafe<T>(PropertyKey<T> propertyKey)
    {
        ArgumentNullException.ThrowIfNull(propertyKey);
        if (propertyKey.IsValueType)
        {
            return Get(propertyKey);
        }

        var result = Get(propertyKey, false) ?? throw new ArgumentException("Unable to retrieve value for [{0}]".ToFormat(propertyKey));
        return (T)result;
    }

    /// <summary>
    /// Gets the specified tag value.
    /// </summary>
    /// <typeparam name="T">Type of the tag value</typeparam>
    /// <param name="propertyKey">The tag property.</param>
    /// <returns>Typed value of the tag property</returns>
    public T? Get<T>(PropertyKey<T> propertyKey)
    {
        ArgumentNullException.ThrowIfNull(propertyKey);
        if (propertyKey.IsValueType)
        {
            // Fast path for value type
            // First, check if there is an accessor
            if (propertyKey.AccessorMetadata != null)
            {
                // TODO: Not optimal, but not used so far
                return (T)propertyKey.AccessorMetadata.GetValue(ref this);
            }


            // Get bound value, if any.
            if (properties != null && properties.TryGetValue(propertyKey, out var value))
                return ((ValueHolder<T>)value).Value;

            if (propertyKey.DefaultValueMetadata != null)
            {
                // Get default value.
                var defaultValue = ((DefaultValueMetadata<T>)propertyKey.DefaultValueMetadata).GetDefaultValueT(ref this);

                // Check if value should be kept.
                if (propertyKey.DefaultValueMetadata.KeepValue)
                {
                    // Register it.
                    Set(propertyKey, defaultValue);
                }

                return defaultValue;
            }

            return default;
        }

        var result = Get(propertyKey, false);
        return result != null ? (T)result : default;
    }

    /// <summary>
    /// Tries to get a tag value.
    /// </summary>
    /// <param name="propertyKey">The tag property.</param>
    /// <param name="value">The value or default vaue if not found</param>
    /// <returns>Returns <c>true</c> if the was found; <c>false</c> otherwise</returns>
    public bool TryGetValue(PropertyKey propertyKey, [MaybeNullWhen(false)] out object value)
    {
        // Implem to avoid boxing/unboxing when using object as output value
        if (ContainsKey(propertyKey))
        {
            value = Get(propertyKey);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Tries to get a tag value.
    /// </summary>
    /// <typeparam name="T">Type of the tag value</typeparam>
    /// <param name="propertyKey">The tag property.</param>
    /// <param name="value">The value or default vaue if not found</param>
    /// <returns>Returns <c>true</c> if the was found; <c>false</c> otherwise</returns>
    public bool TryGetValue<T>(PropertyKey<T> propertyKey, [MaybeNullWhen(false)] out T value)
    {
        if (ContainsKey(propertyKey))
        {
            value = Get(propertyKey);
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Sets the specified tag value.
    /// </summary>
    /// <typeparam name="T">Type of the tag value</typeparam>
    /// <param name="propertyKey">The tag property.</param>
    /// <param name="tagValue">The tag value.</param>
    public void Set<T>(PropertyKey<T> propertyKey, T tagValue)
    {
        if (propertyKey.IsValueType)
        {
            ValueHolder<T>? valueHolder = null;
            T? oldValue;

            // Fast path for value types
            // Fast path for value type
            // First, check if there is an accessor
            if (propertyKey.AccessorMetadata != null)
            {
                // TODO: Not optimal, but not used so far
                oldValue = (T)propertyKey.AccessorMetadata.GetValue(ref this);
            }
            else
            {
                // Get bound value, if any.
                if (properties != null && properties.TryGetValue(propertyKey, out var value))
                {
                    valueHolder = (ValueHolder<T>)value;
                    oldValue = valueHolder.Value;
                }
                else if (propertyKey.DefaultValueMetadata != null)
                {
                    // Get default value.
                    oldValue = propertyKey.DefaultValueMetadataT.GetDefaultValueT(ref this);
                }
                else
                {
                    oldValue = default;
                }
            }

            // Allow to validate the metadata before storing it.
            if (propertyKey.ValidateValueMetadata != null)
            {
                // TODO: Use typed validate?
                propertyKey.ValidateValueMetadataT.ValidateValueCallback(ref tagValue);
            }

            // First, check if there is an accessor
            if (propertyKey.AccessorMetadata != null)
            {
                // TODO: Not optimal, but not used so far
                propertyKey.AccessorMetadata.SetValue(ref this, tagValue);
                return;
            }

            properties ??= [];

            if (valueHolder != null)
                valueHolder.Value = tagValue;
            else
                valueHolder = new ValueHolder<T>(tagValue);

            if (PropertyUpdated != null || propertyKey.PropertyUpdateCallback != null)
            {
                var previousValue = GetNonRecursive(propertyKey);

                properties[propertyKey] = valueHolder;

                if (!ArePropertyValuesEqual(propertyKey, tagValue, previousValue))
                {
                    PropertyUpdated?.Invoke(ref this, propertyKey, tagValue, previousValue);
                    propertyKey.PropertyUpdateCallback?.Invoke(ref this, propertyKey, tagValue, previousValue);
                }
            }
            else
            {
                properties[propertyKey] = valueHolder;
            }

            if (propertyKey.ObjectInvalidationMetadata != null)
            {
                propertyKey.ObjectInvalidationMetadataT.Invalidate(Owner, propertyKey, ref oldValue);
            }

            return;
        }

        SetObject(propertyKey, tagValue, false);
    }

    /// <summary>
    /// Sets the specified tag value.
    /// </summary>
    /// <param name="propertyKey">The tag property.</param>
    /// <param name="tagValue">The tag value.</param>
    public void SetObject(PropertyKey propertyKey, object tagValue)
    {
        SetObject(propertyKey, tagValue, false);
    }

    private void SetObject(PropertyKey propertyKey, object tagValue, bool tryToAdd)
    {
        var oldValue = Get(propertyKey, true);

        // Allow to validate the metadata before storing it.
        propertyKey.ValidateValueMetadata?.Validate(ref tagValue);

        // First, check if there is an accessor
        if (propertyKey.AccessorMetadata != null)
        {
            propertyKey.AccessorMetadata.SetValue(ref this, tagValue);
            return;
        }

        properties ??= [];

        var valueToSet = propertyKey.IsValueType ? propertyKey.CreateValueHolder(tagValue) : tagValue;

        if (PropertyUpdated != null || propertyKey.PropertyUpdateCallback != null)
        {
            var previousValue = GetNonRecursive(propertyKey);

            if (tryToAdd)
                properties.Add(propertyKey, valueToSet);
            else
                properties[propertyKey] = valueToSet;

            if (!ArePropertyValuesEqual(propertyKey, tagValue, previousValue))
            {
                PropertyUpdated?.Invoke(ref this, propertyKey, tagValue, previousValue);
                propertyKey.PropertyUpdateCallback?.Invoke(ref this, propertyKey, tagValue, previousValue);
            }
        }
        else
        {
            if (tryToAdd)
                properties.Add(propertyKey, valueToSet);
            else
                properties[propertyKey] = valueToSet;
        }

        propertyKey.ObjectInvalidationMetadata?.Invalidate(Owner, propertyKey, oldValue);
    }

    public static void AddAccessorProperty(Type type, PropertyKey propertyKey)
    {
        if (!type.GetTypeInfo().IsClass)
            throw new ArgumentException("Class type is expected.", nameof(type));

        if (propertyKey.AccessorMetadata == null)
            throw new ArgumentException("Given PropertyKey doesn't have accessor metadata.", nameof(propertyKey));

        if (!AccessorProperties.TryGetValue(type, out var typeAccessorProperties))
            AccessorProperties.Add(type, typeAccessorProperties = []);

        typeAccessorProperties.Add(propertyKey);
    }

    internal void RaisePropertyContainerUpdated(PropertyKey propertyKey, object newValue, object oldValue)
    {
        PropertyUpdated?.Invoke(ref this, propertyKey, newValue, oldValue);
    }

    private object? GetNonRecursive(PropertyKey propertyKey)
    {
        // Get bound value, if any.
        if (properties != null && properties.TryGetValue(propertyKey, out var value))
        {
            return propertyKey.IsValueType ? ((ValueHolder)value).ObjectValue : value;
        }

        // Get default value.
        return propertyKey.DefaultValueMetadata?.GetDefaultValue(ref this);
    }

    private static bool ArePropertyValuesEqual(PropertyKey propertyKey, object? propertyValue1, object? propertyValue2)
    {
        var propertyType = propertyKey.PropertyType;

        if (!propertyType.GetTypeInfo().IsValueType && propertyType != typeof(string))
        {
            return ReferenceEquals(propertyValue1, propertyValue2);
        }

        return Equals(propertyValue1, propertyValue2);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    void ICollection<KeyValuePair<PropertyKey, object>>.Add(KeyValuePair<PropertyKey, object> item)
    {
        SetObject(item.Key, item.Value);
    }

    readonly bool ICollection<KeyValuePair<PropertyKey, object>>.Contains(KeyValuePair<PropertyKey, object> item)
    {
        return properties is not null && properties.TryGetValue(item.Key, out var temp) && Equals(temp, item.Value);
    }

    readonly void ICollection<KeyValuePair<PropertyKey, object>>.CopyTo(KeyValuePair<PropertyKey, object>[] array, int arrayIndex)
    {
        ((IDictionary<PropertyKey, object>?)properties)?.CopyTo(array, arrayIndex);
    }

    bool ICollection<KeyValuePair<PropertyKey, object>>.Remove(KeyValuePair<PropertyKey, object> item)
    {
        return Remove(item.Key);
    }

    readonly IEnumerable<PropertyKey> IReadOnlyDictionary<PropertyKey, object>.Keys => Keys;

    readonly IEnumerable<object> IReadOnlyDictionary<PropertyKey, object>.Values => Values;

    internal abstract class ValueHolder
    {
        public abstract object ObjectValue { get; }
    }

    internal class ValueHolder<T> : ValueHolder
    {
        public T Value;

        public ValueHolder(T value)
        {
            Value = value;
        }

        public override object ObjectValue => Value;
    }
}
