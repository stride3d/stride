// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Core
{
    /// <summary>
    /// Describes the platform operating system.
    /// </summary>
#if XENKO_ASSEMBLY_PROCESSOR
    // To avoid a CS1503 error when compiling projects that are using both the AssemblyProcessor
    // and Xenko.Core.
    internal enum PlatformType
#else
    [DataContract("PlatformType")]
    public enum PlatformType
#endif

    {
        // ***************************************************************
        // NOTE: This file is shared with the AssemblyProcessor.
        // If this file is modified, the AssemblyProcessor has to be
        // recompiled separately. See build\Xenko-AssemblyProcessor.sln
        // ***************************************************************

        /// <summary>
        /// This is shared across platforms
        /// </summary>
        Shared,

        /// <summary>
        /// The windows desktop OS.
        /// </summary>
        Windows,

        /// <summary>
        /// The android OS.
        /// </summary>
        Android,

        /// <summary>
        /// The iOS.
        /// </summary>
        iOS,

        /// <summary>
        /// The Universal Windows Platform (UWP).
        /// </summary>
        UWP,

        /// <summary>
        /// The Linux OS.
        /// </summary>
        Linux,

        /// <summary>
        /// macOS
        /// </summary>
        macOS,

        /// <summary>
        /// The Universal Windows Platform (UWP). Please use <see cref="UWP"/> intead.
        /// </summary>
        [Obsolete("Please use UWP instead")]
        Windows10 = UWP,
    }
}
