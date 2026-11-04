// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Serialization;

/// <summary>
/// A null serializer that can be used to add dummy serialization attributes.
/// </summary>
public sealed class NullSerializer<T> : DataSerializer<T>
{
    /// <summary>
    /// Gets the shared instance of this serializer.
    /// </summary>
    public static NullSerializer<T> Instance { get; } = new();

    public override void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)
    {
        throw new NotSupportedException($"No serializer available for type {typeof(T).FullName}.");
    }
}
