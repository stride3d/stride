// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.TextureConverter.Tests
{
    internal static class TextureToolExtensions
    {
        public static TexImage Load(this TextureTool tool, string file)
        {
            return tool.Load(file, false);
        }
    }
}
