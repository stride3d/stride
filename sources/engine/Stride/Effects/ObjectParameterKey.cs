// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Rendering;

[DataSerializer(typeof(ObjectParameterKeySerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
public sealed class ObjectParameterKey<T> : ParameterKey<T>
{
    public ObjectParameterKey(string name, int length = 1, PropertyKeyMetadata? metadata = null)
        : base(ParameterKeyType.Object, name, length, metadata)
    {
    }

    public ObjectParameterKey(string name, int length = 1, params PropertyKeyMetadata[]? metadatas)
        : base(ParameterKeyType.Object, name, length, metadatas)
    {
    }
}
