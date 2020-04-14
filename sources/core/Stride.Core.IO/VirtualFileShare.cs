// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core.IO
{
    /// <summary>
    /// File share capabilities, equivalent of <see cref="System.IO.FileShare"/>.
    /// </summary>
    [Flags]
    public enum VirtualFileShare : uint
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = 3,
        Delete = 4,
    }
}
