// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Serialization;

internal static class StringHashHelper
{
    public static uint GetSerializerHashCode(this string param)
    {
        uint result = 0;
        foreach (var c in param)
        {
            result ^= result << 4;
            result ^= result << 24;
            result ^= c;
        }
        return result;
    }
}
