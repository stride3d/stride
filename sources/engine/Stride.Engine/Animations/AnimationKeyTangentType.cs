﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type

namespace Stride.Animations
{
    public enum AnimationKeyTangentType
    {
        /// <summary>
        /// Linear - the value is linearly calculated as V1 * (1 - t) + V2 * t
        /// </summary>
        Linear,        
    }
}
