// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1300 // Element should begin with upper-case letter
using System;
using Xenko.Core;

namespace Xenko.Data
{
    [Flags]
    public enum ConfigPlatforms
    {
        None = 0,
        Windows = 1 << PlatformType.Windows,
        UWP = 1 << PlatformType.UWP,
        iOS = 1 << PlatformType.iOS,
        Android = 1 << PlatformType.Android,
        Linux = 1 << PlatformType.Linux,
        macOS = 1 << PlatformType.macOS,
    }
}
