// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Graphics
{
    /// <summary>
    /// A sheet (group) of sprites.
    /// </summary>
    [DataContract]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<SpriteSheet>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<SpriteSheet>))]
    public class SpriteSheet
    {
        /// <summary>
        /// The list of sprites.
        /// </summary>
        [MemberCollection(NotNullItems = true)]
        public List<Sprite> Sprites { get; } = new List<Sprite>();

        /// <summary>
        /// Find the index of a sprite in the group using its name.
        /// </summary>
        /// <param name="spriteName">The name of the sprite</param>
        /// <returns>The index value</returns>
        /// <remarks>If two sprites have the provided name then the first sprite found is returned</remarks>
        /// <exception cref="KeyNotFoundException">No sprite in the group have the given name</exception>
        public int FindImageIndex(string spriteName)
        {
            if (Sprites != null)
            {
                for (int i = 0; i < Sprites.Count; i++)
                {
                    if (Sprites[i].Name == spriteName)
                        return i;
                }
            }

            throw new KeyNotFoundException(spriteName);
        }

        /// <summary>
        /// Gets or sets the image of the group at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The image index</param>
        /// <returns>The image</returns>
        public Sprite this[int index]
        {
            get { return Sprites[index]; }
            set { Sprites[index] = value; }
        }

        /// <summary>
        /// Gets or sets the image of the group having the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the image</param>
        /// <returns>The image</returns>
        public Sprite this[string name]
        {
            get { return Sprites[FindImageIndex(name)]; }
            set { Sprites[FindImageIndex(name)] = value; }
        }
    }
}
