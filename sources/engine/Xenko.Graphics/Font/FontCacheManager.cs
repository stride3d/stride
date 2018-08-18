// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Graphics.Font
{
    /// <summary>
    /// Represent a GPU cache of font characters
    /// </summary>
    internal class FontCacheManager : ComponentBase
    {
        private readonly FontSystem system;

        private readonly List<Texture> cacheTextures = new List<Texture>();
        private readonly LinkedList<CharacterSpecification> cachedCharacters = new LinkedList<CharacterSpecification>();
        private readonly GuillotinePacker packer = new GuillotinePacker();
        
        /// <summary>
        /// The textures containing the cached characters on the GPU.
        /// </summary>
        public IReadOnlyList<Texture> Textures { get; private set; }

        public FontCacheManager(FontSystem system, int textureDefaultSize = 1024)
        {
            this.system = system;
            Textures = cacheTextures;

            // create the cache textures
            var newTexture = Texture.New2D(system.GraphicsDevice, textureDefaultSize, textureDefaultSize, PixelFormat.R8_UNorm);
            cacheTextures.Add(newTexture);
            newTexture.Reload = ReloadCache;
            ClearCache();
        }

        private void ReloadCache(GraphicsResourceBase graphicsResourceBase)
        {
            foreach (var cacheTexture in cacheTextures)
                cacheTexture.Recreate();

            ClearCache();
        }

        /// <summary>
        /// Remove all the currently cached characters from the cache.
        /// </summary>
        public void ClearCache()
        {
            foreach (var character in cachedCharacters)
                character.IsBitmapUploaded = false;
            cachedCharacters.Clear();

            packer.Clear(cacheTextures[0].ViewWidth, cacheTextures[0].ViewHeight);
        }
        
        /// <summary>
        /// Upload a character's bitmap into the current cache.
        /// </summary>
        /// <param name="character">The character specifications corresponding to the bitmap</param>
        public void UploadCharacterBitmap(CommandList commandList, CharacterSpecification character)
        {
            if (character.Bitmap == null)
                throw new ArgumentNullException("character");

            if (character.IsBitmapUploaded)
                throw new InvalidOperationException($"The character '{character.Character}' upload has been requested while its current glyph is valid.");

            var targetSize = new Int2(character.Bitmap.Width, character.Bitmap.Rows);
            if (!packer.Insert(targetSize.X, targetSize.Y, ref character.Glyph.Subrect))
            {
                // not enough space to place the new character -> remove less used characters and try again
                RemoveLessUsedCharacters();
                if (!packer.Insert(targetSize.X, targetSize.Y, ref character.Glyph.Subrect))
                {
                    // memory is too fragmented in order to place the new character -> clear all the characters and restart.
                    ClearCache();
                    if (!packer.Insert(targetSize.X, targetSize.Y, ref character.Glyph.Subrect))
                        throw new InvalidOperationException("The rendered character is too big for the cache texture");
                }
            }
            // updload the bitmap on the texture (if the size in the bitmap is not null)
            if (character.Bitmap.Rows != 0 && character.Bitmap.Width != 0)
            {
                var dataBox = new DataBox(character.Bitmap.Buffer, character.Bitmap.Pitch, character.Bitmap.Pitch * character.Bitmap.Rows);
                var region = new ResourceRegion(character.Glyph.Subrect.Left, character.Glyph.Subrect.Top, 0, character.Glyph.Subrect.Right, character.Glyph.Subrect.Bottom, 1);
                commandList.UpdateSubresource(cacheTextures[0], 0, dataBox, region);
            }

            // update the glyph data
            character.IsBitmapUploaded = true;
            character.Glyph.BitmapIndex = 0;
        }

        /// <summary>
        /// Remove all the character that haven't been used for the given amount of frames
        /// </summary>
        private void RemoveLessUsedCharacters(int frameCount = 5)
        {
            var limitFrame = system.FrameCount - frameCount;
            var currentNode = cachedCharacters.Last;
            while (currentNode != null && currentNode.Value.LastUsedFrame < limitFrame)
            {
                currentNode.Value.IsBitmapUploaded = false;
                packer.Free(ref currentNode.Value.Glyph.Subrect);
                currentNode = currentNode.Previous;
                cachedCharacters.RemoveLast();
            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            foreach (var cacheTexture in cacheTextures)
                cacheTexture.Dispose();

            cacheTextures.Clear();
            cachedCharacters.Clear();
        }

        public void NotifyCharacterUtilization(CharacterSpecification character)
        {
            character.LastUsedFrame = system.FrameCount;

            if (character.ListNode.List != null)
                cachedCharacters.Remove(character.ListNode);
            cachedCharacters.AddFirst(character.ListNode);
        }
    }
}
