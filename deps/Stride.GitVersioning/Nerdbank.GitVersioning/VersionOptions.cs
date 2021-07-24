// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Nerdbank.GitVersioning
{
    /// <summary>
    /// Store package version read from .sdpkg, implemented for <see cref="GitExtensions"/>.
    /// </summary>
    class VersionOptions
    {
        public int BuildNumberOffset => 0;

        public Version Version { get; set; }
    }
}
