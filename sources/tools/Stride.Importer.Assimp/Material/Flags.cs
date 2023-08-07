// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Importer.Assimp.Material
{
    /// <summary>
    /// Enumeration of the new Assimp's flags.
    /// </summary>

    [Flags]
    public enum Flags
    {
        None = 0,
        Invert = 1,
        ReplaceAlpha = 2
    }
}
