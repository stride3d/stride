// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core.Assets.Editor.Components.Properties
{
    [Flags]
    public enum TargetPackage
    {
        None = 0,
        Executable = 1,
        NonExecutable = 2,
        All = Executable | NonExecutable
    }
}
