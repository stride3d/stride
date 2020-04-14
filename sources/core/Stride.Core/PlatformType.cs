// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core
{
    /// <summary>
    /// Describes the platform operating system.
    /// </summary>
#if STRIDE_ASSEMBLY_PROCESSOR
    // To avoid a CS1503 error when compiling projects that are using both the AssemblyProcessor
    // and Stride.Core.
    internal enum PlatformType
#else
    [DataContract("PlatformType")]
    public enum PlatformType
#endif
    {
        // ***************************************************************
        // NOTE: This file is shared with the AssemblyProcessor.
        // If this file is modified, the AssemblyProcessor has to be
        // recompiled separately. See build\Stride-AssemblyProcessor.sln
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

#pragma warning disable SA1300 // Element must begin with upper-case letter
        /// <summary>
        /// The iOS.
        /// </summary>
        iOS,
#pragma warning restore SA1300 // Element must begin with upper-case letter

        /// <summary>
        /// The Universal Windows Platform (UWP).
        /// </summary>
        UWP,

        /// <summary>
        /// The Linux OS.
        /// </summary>
        Linux,

#pragma warning disable SA1300 // Element must begin with upper-case letter
        /// <summary>
        /// macOS
        /// </summary>
        macOS,
#pragma warning restore SA1300 // Element must begin with upper-case letter

        /// <summary>
        /// The Universal Windows Platform (UWP). Please use <see cref="UWP"/> intead.
        /// </summary>
        [Obsolete("Please use UWP instead")]
        Windows10 = UWP,
    }
}
