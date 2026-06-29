// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Buffers;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics.Font
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

        private void ReloadCache(GraphicsResourceBase graphicsResourceBase, IServiceRegistry services)
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
                    // TODO: This is invalid, we might delete character from current frame!
                    ClearCache();
                    if (!packer.Insert(targetSize.X, targetSize.Y, ref character.Glyph.Subrect))
                        throw new InvalidOperationException("The rendered character is too big for the cache texture");
                }
            }
            // Upload the bitmap to the atlas texture, with a 1px transparent border extending
            // beyond the allocated region to clear any stale pixels from previously freed glyphs.
            // This overlaps into neighbors' transparent borders (which are also zeroed), so no
            // glyph data is corrupted. Prevents bilinear filtering artifacts when scaling fonts.
            if (character.Bitmap.Rows != 0 && character.Bitmap.Width != 0)
            {
                var texW = cacheTextures[0].ViewWidth;
                var texH = cacheTextures[0].ViewHeight;

                // Expand the upload region by 1px on each side, clamped to texture bounds
                int left = Math.Max(0, character.Glyph.Subrect.Left - 1);
                int top = Math.Max(0, character.Glyph.Subrect.Top - 1);
                int right = Math.Min(texW, character.Glyph.Subrect.Right + 1);
                int bottom = Math.Min(texH, character.Glyph.Subrect.Bottom + 1);
                int expandedW = right - left;
                int expandedH = bottom - top;

                // Build expanded buffer: zero-filled, with the glyph bitmap copied into the center
                var expandedSize = expandedW * expandedH;
                var expandedBuffer = ArrayPool<byte>.Shared.Rent(expandedSize);
                Array.Clear(expandedBuffer, 0, expandedSize);
                int offsetX = character.Glyph.Subrect.Left - left;
                int offsetY = character.Glyph.Subrect.Top - top;
                unsafe
                {
                    var src = (byte*)character.Bitmap.Buffer;
                    for (int y = 0; y < character.Bitmap.Rows; y++)
                    {
                        var srcRow = src + y * character.Bitmap.Pitch;
                        var dstOffset = (y + offsetY) * expandedW + offsetX;
                        for (int x = 0; x < character.Bitmap.Width; x++)
                            expandedBuffer[dstOffset + x] = srcRow[x];
                    }

                    fixed (byte* pExpanded = expandedBuffer)
                    {
                        var dataBox = new DataBox((nint)pExpanded, expandedW, expandedSize);
                        var region = new ResourceRegion(left, top, 0, right, bottom, 1);
                        commandList.UpdateSubResource(cacheTextures[0], 0, dataBox, region);
                    }
                }
                ArrayPool<byte>.Shared.Return(expandedBuffer);

                // UpdateSubResource leaves the atlas in CopyDest; transition back so the next
                // sprite-batch sample sees ShaderResource without relying on a lazy transition.
                commandList.ResourceBarrierTransition(cacheTextures[0], BarrierLayout.ShaderResource);
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
