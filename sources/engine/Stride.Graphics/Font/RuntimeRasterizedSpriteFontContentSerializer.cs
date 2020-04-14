// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Serialization.Contents;

namespace Xenko.Graphics.Font
{
    internal class RuntimeRasterizedSpriteFontContentSerializer : DataContentSerializer<RuntimeRasterizedSpriteFont>
    {
        public override object Construct(ContentSerializerContext context)
        {
            return new RuntimeRasterizedSpriteFont();
        }
    }
}
