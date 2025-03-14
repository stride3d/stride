// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class

namespace Stride.Core;

/// <summary>
/// Delegate ObjectInvalidatorCallback used by <see cref="ObjectInvalidationMetadata"/>.
/// </summary>
/// <param name="propertyOwner">The owner of the property that changed.</param>
/// <param name="propertyKey">The key of the property that changed.</param>
/// <param name="propertyOldValue">The value of the property before its modification.</param>
public delegate void ObjectInvalidationCallback<T>(object propertyOwner, PropertyKey<T> propertyKey, T? propertyOldValue);

public delegate void ObjectInvalidationRefCallback<T>(object propertyOwner, PropertyKey<T> propertyKey, ref T? propertyOldValue);

public abstract class ObjectInvalidationMetadata : PropertyKeyMetadata
{
    public static ObjectInvalidationMetadata New<T>(ObjectInvalidationCallback<T> invalidationCallback)
    {
        return new ObjectInvalidationMetadata<T>(invalidationCallback);
    }

    public static ObjectInvalidationMetadata NewRef<T>(ObjectInvalidationRefCallback<T> invalidationRefCallback)
    {
        return new ObjectInvalidationMetadata<T>(invalidationRefCallback);
    }

    public abstract void Invalidate(object propertyOwner, PropertyKey propertyKey, object? propertyOldValue);
}

/// <summary>
/// Metadata used to invalidate an object state after a property value modification.
/// </summary>
public class ObjectInvalidationMetadata<T> : ObjectInvalidationMetadata
{
    private readonly ObjectInvalidationCallback<T>? objectInvalidationCallback;
    private readonly ObjectInvalidationRefCallback<T> objectInvalidationRefCallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectInvalidationMetadata{T}"/> class.
    /// </summary>
    /// <param name="invalidationCallback">The object invalidation callback.</param>
    /// <exception cref="ArgumentNullException">Parameter <paramref name="invalidationCallback"/> is null.</exception>
    public ObjectInvalidationMetadata(ObjectInvalidationCallback<T> invalidationCallback)
    {
        ArgumentNullException.ThrowIfNull(invalidationCallback);
        objectInvalidationCallback = invalidationCallback;
        objectInvalidationRefCallback = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectInvalidationMetadata{T}"/> class.
    /// </summary>
    /// <param name="invalidationRefCallback">The object invalidation callback.</param>
    /// <exception cref="ArgumentNullException">Parameter <paramref name="invalidationRefCallback"/> is null.</exception>
    public ObjectInvalidationMetadata(ObjectInvalidationRefCallback<T> invalidationRefCallback)
    {
        ArgumentNullException.ThrowIfNull(invalidationRefCallback);
        objectInvalidationRefCallback = invalidationRefCallback;
    }

    public void Invalidate(object propertyOwner, PropertyKey<T> propertyKey, ref T? propertyOldValue)
    {
        if (objectInvalidationCallback != null)
            objectInvalidationCallback(propertyOwner, propertyKey, propertyOldValue);
        else
            objectInvalidationRefCallback(propertyOwner, propertyKey, ref propertyOldValue);
    }

    public override void Invalidate(object propertyOwner, PropertyKey propertyKey, object? propertyOldValue)
    {
        var propertyOldValueT = (T?)propertyOldValue;

        if (objectInvalidationCallback != null)
            objectInvalidationCallback(propertyOwner, (PropertyKey<T>)propertyKey, propertyOldValueT);
        else
            objectInvalidationRefCallback(propertyOwner, (PropertyKey<T>)propertyKey, ref propertyOldValueT);
    }
}
