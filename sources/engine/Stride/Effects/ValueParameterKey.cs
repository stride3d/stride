// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Rendering
{
    [DataSerializer(typeof(ValueParameterKeySerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    public sealed class ValueParameterKey<T> : ParameterKey<T> where T : struct
    {
        public ValueParameterKey(string name, int length = 1, PropertyKeyMetadata? metadata = null)
            : base(ParameterKeyType.Value, name, length, metadata)
        {
        }

        public ValueParameterKey(string name, int length = 1, params PropertyKeyMetadata[]? metadatas)
            : base(ParameterKeyType.Value, name, length, metadatas)
        {
        }


        internal override unsafe object ReadValue(nint data)
            => Unsafe.ReadUnaligned<T>((void*) data);

        internal override object ReadValue(scoped ref readonly byte data)
            => Unsafe.ReadUnaligned<T>(in data);

        internal override object ReadValue(scoped ReadOnlySpan<byte> data)
            => Unsafe.ReadUnaligned<T>(in data[0]);
    }
}
