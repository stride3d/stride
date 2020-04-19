// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Graphics
{
    internal class ImageSerializer : ContentSerializerBase<Image>
    {
        public override void Serialize(ContentSerializerContext context, SerializationStream stream, Image textureData)
        {
            if (context.Mode == ArchiveMode.Deserialize)
            {
                var image = Image.Load(stream.NativeStream);
                textureData.InitializeFrom(image);
            }
            else
            {
                textureData.Save(stream.NativeStream, ImageFileType.Stride);
            }
        }

        public override object Construct(ContentSerializerContext context)
        {
            return new Image();
        }
    }
}
