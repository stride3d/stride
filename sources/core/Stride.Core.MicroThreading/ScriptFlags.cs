// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.MicroThreading
{
    [Flags]
    public enum ScriptFlags
    {
        /// <summary>
        /// Empty value.
        /// </summary>
        None = 0,

        /// <summary>
        /// Automatically run on assembly startup.
        /// </summary>
        AssemblyStartup = 1,

        /// <summary>
        /// Automatically run on assembly first startup (not executed if assembly is reloaded).
        /// </summary>
        AssemblyFirstStartup = 2,

        /// <summary>
        /// Automatically run on assembly unload.
        /// </summary>
        AssemblyUnload = 4,

        // TODO: Not implemented yet
        /// <summary>
        /// MicroThread won't be killed if assembly is unloaded (including reload).
        /// </summary>
        KeepAliveWhenUnload = 8,
    }
}
