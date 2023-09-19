// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Importer.Assimp.Material
{
    /// <summary>
    /// Enumeration of the different types of node in the new Assimp's material stack.
    /// </summary>
    public enum StackElementType
    {
        Color = 0,
        Texture,
        Operation
    }
}
