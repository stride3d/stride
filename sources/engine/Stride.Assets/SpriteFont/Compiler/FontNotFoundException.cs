// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Assets.SpriteFont.Compiler
{
    internal class FontNotFoundException : Exception
    {
        public FontNotFoundException(string fontName) : base($"Font with name [{fontName}] not found on this machine")
        {
            FontName = fontName;
        }

        public string FontName { get; private set; }
    }
}
