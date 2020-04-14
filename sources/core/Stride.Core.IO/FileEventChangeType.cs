// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.IO
{
    /// <summary>
    /// Change type of file used by <see cref="FileEvent"/> and <see cref="DirectoryWatcher"/>.
    /// </summary>
    [Flags]
    public enum FileEventChangeType
    {
        // This enum must match exactly the System.IO.WatcherChangeTypes

        Created = 1,
        Deleted = 2,
        Changed = 4,
        Renamed = 8,
        All = Renamed | Changed | Deleted | Created,
    }
}
