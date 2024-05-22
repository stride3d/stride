using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection.TypeDescriptors;
internal class SimpleDictionaryDescriptor : DictionaryDescriptor
{
    public SimpleDictionaryDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention) : base(factory, type, emitDefaultValues, namingConvention)
    {
    }
    public override DescriptorCategory Category => DescriptorCategory.Dictionary;

    /// <summary>
    /// Gets a value indicating whether this instance is generic dictionary.
    /// </summary>
    /// <value><c>true</c> if this instance is generic dictionary; otherwise, <c>false</c>.</value>
    public override bool IsGenericDictionary { get; } = false;

    /// <summary>
    /// Gets the type of the key.
    /// </summary>
    /// <value>The type of the key.</value>
    public override Type KeyType { get; protected init; } = typeof(object);

    /// <summary>
    /// Gets the type of the value.
    /// </summary>
    /// <value>The type of the value.</value>
    public override Type ValueType { get; protected init; } = typeof(object);

    /// <summary>
    /// Determines whether the value passed is readonly.
    /// </summary>
    /// <param name="thisObject">The this object.</param>
    /// <returns><c>true</c> if [is read only] [the specified this object]; otherwise, <c>false</c>.</returns>
    public override bool IsReadOnly(object thisObject)
    {
        return ((IDictionary)thisObject).IsReadOnly;
    }

    /// <summary>
    /// Gets a generic enumerator for a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <returns>A generic enumerator.</returns>
    /// <exception cref="System.ArgumentNullException">dictionary</exception>
    public override IEnumerable<KeyValuePair<object, object?>> GetEnumerator(object dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        var simpleDictionary = (IDictionary)dictionary;
        foreach (var keyValueObject in simpleDictionary)
        {
            if (keyValueObject is not DictionaryEntry)
            {
                throw new NotSupportedException($"Key value-pair type [{keyValueObject}] is not supported for IDictionary. Only DictionaryEntry");
            }
            var entry = (DictionaryEntry)keyValueObject;
            yield return new KeyValuePair<object, object?>(entry.Key, entry.Value);
        }
    }

    /// <summary>
    /// Adds a a key-value to a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="System.InvalidOperationException">No Add() method found on dictionary [{0}].ToFormat(Type)</exception>
    public override void SetValue(object dictionary, object key, object value)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ((IDictionary)dictionary)[key] = value;
    }

    /// <summary>
    /// Adds a a key-value to a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="System.InvalidOperationException">No Add() method found on dictionary [{0}].DoFormat(Type)</exception>
    public override void AddToDictionary(object dictionary, object key, object value)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ((IDictionary)dictionary).Add(key, value);
    }

    /// <summary>
    /// Remove a key-value from a dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    public override void Remove(object dictionary, object key)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ((IDictionary)dictionary).Remove(key);
    }

    /// <summary>
    /// Indicate whether the dictionary contains the given key
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    public override bool ContainsKey(object dictionary, object key)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return ((IDictionary)dictionary).Contains(key);
    }

    /// <summary>
    /// Returns an enumerable of the keys in the dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary</param>
    public override ICollection GetKeys(object dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return ((IDictionary)dictionary).Keys;
    }

    /// <summary>
    /// Returns an enumerable of the values in the dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary</param>
    public override ICollection GetValues(object dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return ((IDictionary)dictionary).Values;
    }

    /// <summary>
    /// Returns the value matching the given key in the dictionary, or null if the key is not found
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    public override object? GetValue(object dictionary, object key)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return ((IDictionary)dictionary)[key];
    }
}
