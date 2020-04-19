// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Graphics.Data
{
    /// <summary>
    /// Serializer for <see cref="Texture"/>.
    /// </summary>
    public class TextureSerializer : DataSerializer<Texture>
    {
        /// <inheritdoc/>
        public override void PreSerialize(ref Texture texture, ArchiveMode mode, SerializationStream stream)
        {
            // Do not create object during preserialize (OK because not recursive)
        }
        
        /// <inheritdoc/>
        public override void Serialize(ref Texture texture, ArchiveMode mode, SerializationStream stream)
        {
            TextureContentSerializer.Serialize(mode, stream, texture, false);
        }
    }
}
