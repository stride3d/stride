// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Core.Serialization.Binary;

public static class StrideSerializationExtensions
{
    public static unsafe void Serialize(this SerializationStream serializer, nint ptr, int length)
        => serializer.Serialize((void*)ptr, length);
    public static unsafe void Serialize(this SerializationStream serializer, void* ptr, int length)
        => serializer.Serialize(new Span<byte>(ptr, length));

}
