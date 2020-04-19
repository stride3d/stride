// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.IO
{
    [Flags]
    public enum VirtualWatcherChangeTypes
    {
        Created = 1,

        Deleted = 2,

        Changed = 4,

        Renamed = 8,

        All = 15,
    }
}
