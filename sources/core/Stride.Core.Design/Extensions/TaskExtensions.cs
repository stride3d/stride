// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core.Extensions;

public static class TaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forget(this Task task)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(task);
#else
        if (task is null) throw new ArgumentNullException(nameof(task));
#endif
    }
}
