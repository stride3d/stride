// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization.Contents;

namespace Stride.Graphics.Font
{
    internal class RuntimeRasterizedSpriteFontContentSerializer : DataContentSerializer<RuntimeRasterizedSpriteFont>
    {
        public override object Construct(ContentSerializerContext context)
        {
            return new RuntimeRasterizedSpriteFont();
        }
    }
}
