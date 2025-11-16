// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering;

public readonly struct ValueParameterAccessor<T> where T : unmanaged
{
    public readonly int Offset;
    public readonly int Count;


    internal ValueParameterAccessor(int offset, int count)
    {
        Offset = offset;
        Count = count;
    }
}
