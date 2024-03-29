﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Profiling
{
    public enum GameProfilingSorting
    {
        [Display("Average time")]
        ByAverageTime,

        [Display("Total time")]
        ByTime,

        [Display("Key")]
        ByName,
    }
}
