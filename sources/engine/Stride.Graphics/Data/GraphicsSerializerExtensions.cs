// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Graphics.Data
{
    /// <summary>
    /// Various extension method for serialization of GPU types having separate CPU serialized data format.
    /// </summary>
    public static class GraphicsSerializerExtensions
    {
        /// <summary>
        /// Creates a fake <see cref="Buffer" /> that will have the given serialized data version.
        /// </summary>
        /// <param name="bufferData">The buffer data.</param>
        /// <returns></returns>
        public static Buffer ToSerializableVersion(this BufferData bufferData)
        {
            var buffer = new Buffer();
            buffer.SetSerializationData(bufferData);

            return buffer;
        }

        /// <summary>
        /// Gets the serialized data version of this <see cref="Buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public static BufferData GetSerializationData(this Buffer buffer)
        {
            var attachedReference = AttachedReferenceManager.GetAttachedReference(buffer);
            return attachedReference != null ? (BufferData)attachedReference.Data : null;
        }

        /// <summary>
        /// Sets the serialized data version of this <see cref="Buffer" />.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bufferData">The buffer data.</param>
        public static void SetSerializationData(this Buffer buffer, BufferData bufferData)
        {
            var attachedReference = AttachedReferenceManager.GetOrCreateAttachedReference(buffer);
            attachedReference.Data = bufferData;
        }

        /// <summary>
        /// Creates a fake <see cref="Texture"/> that will have the given serialized data version.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns></returns>
        public static Texture ToSerializableVersion(this Image image)
        {
            var texture = new Texture();
            texture.SetSerializationData(image);
            return texture;
        }

        /// <summary>
        /// Creates a fake <see cref="Texture"/> that will have the given serialized data version.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static Texture ToSerializableVersion(this TextureSerializationData data)
        {
            var texture = new Texture();
            texture.SetSerializationData(data);
            return texture;
        }

        /// <summary>
        /// Gets the serialized data version of this <see cref="Texture"/>.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <returns></returns>
        public static TextureSerializationData GetSerializationData(this Texture texture)
        {
            var attachedReference = AttachedReferenceManager.GetAttachedReference(texture);
            return (TextureSerializationData)attachedReference?.Data;
        }

        /// <summary>
        /// Sets the serialized data version of this <see cref="Texture" />.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="image">The image.</param>
        public static void SetSerializationData(this Texture texture, Image image)
        {
            texture.SetSerializationData(new TextureSerializationData(image));
        }

        /// <summary>
        /// Sets the serialized data version of this <see cref="Texture" />.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="data">The data.</param>
        public static void SetSerializationData(this Texture texture, TextureSerializationData data)
        {
            var attachedReference = AttachedReferenceManager.GetOrCreateAttachedReference(texture);
            attachedReference.Data = data;
        }
    }
}
