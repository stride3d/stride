// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Diagnostics
{
    [Flags]
    public enum ProfilingKeyFlags
    {
        /// <summary>
        /// Empty flag.
        /// </summary>
        None = 0,

        /// <summary>
        /// Output message to log right away.
        /// </summary>
        Log = 1,
    }
}
