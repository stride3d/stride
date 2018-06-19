// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core.Assets.Editor.Components.Properties
{
    [Flags]
    public enum TargetProfile
    {
        None = 0,
        Shared = 1,
        Platform = 2,
        All = Shared | Platform
    }
}
