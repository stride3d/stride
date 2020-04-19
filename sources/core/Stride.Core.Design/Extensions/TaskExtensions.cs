// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Core.Annotations;

namespace Stride.Core.Extensions
{
    public static class TaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Forget([NotNull] this Task task)
        {
            if (task == null) throw new ArgumentNullException();
        }
    }
}
