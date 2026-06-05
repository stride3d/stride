// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Stride.Core.Serialization.Contents;

/// <summary>
/// Base class for Content Serializer with empty virtual implementation.
/// </summary>
/// <typeparam name="T">Runtime type being serialized.</typeparam>
public class ContentSerializerBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : IContentSerializer<T>
{
    private static readonly bool HasParameterlessConstructor = typeof(T).GetConstructor(Type.EmptyTypes) is not null;

    /// <inheritdoc/>
    public virtual Type SerializationType
    {
        get { return typeof(T); }
    }

    /// <inheritdoc/>
    public virtual Type ActualType
    {
        get { return typeof(T); }
    }

    /// <inheritdoc/>
    public virtual object Construct(ContentSerializerContext context)
    {
        return HasParameterlessConstructor ? Activator.CreateInstance<T>() : default;
    }

    /// <inheritdoc/>
    public virtual void Serialize(ContentSerializerContext context, SerializationStream stream, T obj)
    {
    }

    /// <inheritdoc/>
    public void Serialize(ContentSerializerContext context, SerializationStream stream, object obj)
    {
        var objT = (T)obj;
        Serialize(context, stream, objT);
    }
}
