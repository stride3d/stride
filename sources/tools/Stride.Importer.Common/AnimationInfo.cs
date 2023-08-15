// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Animations;

namespace Stride.Importer.Common
{
    public class AnimationInfo
    {
        public TimeSpan Duration;
        public Dictionary<string, AnimationClip> AnimationClips = new();
    }
}
