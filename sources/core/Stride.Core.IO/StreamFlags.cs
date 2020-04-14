// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.IO
{
    /// <summary>
    /// Describes the different type of streams.
    /// </summary>
    [Flags]
    public enum StreamFlags
    {
        /// <summary>
        /// Returns the default underlying stream without any alterations. Can be a seek-able stream or not depending on the file.
        /// </summary>
        None,

        /// <summary>
        /// A stream in which we can seek
        /// </summary>
        Seekable,
    }
}
