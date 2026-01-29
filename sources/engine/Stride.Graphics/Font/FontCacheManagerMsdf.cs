// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// GPU cache for RGBA glyphs (intended for runtime MSDF).
    /// Parallel to <see cref="FontCacheManager"/> to keep the R8 path unchanged.
    /// </summary>
    internal class FontCacheManagerMsdf : ComponentBase
    {
        private readonly FontSystem system;

        private readonly List<Texture> cacheTextures = new List<Texture>();
        private readonly LinkedList<MsdfCachedGlyph> cachedGlyphs = new LinkedList<MsdfCachedGlyph>();
        private readonly GuillotinePacker packer = new GuillotinePacker();

        public IReadOnlyList<Texture> Textures { get; private set; }

        public FontCacheManagerMsdf(FontSystem system, int textureDefaultSize = 1024)
        {
            this.system = system ?? throw new ArgumentNullException(nameof(system));
            Textures = cacheTextures;

            var newTexture = Texture.New2D(system.GraphicsDevice, textureDefaultSize, textureDefaultSize, PixelFormat.R8G8B8A8_UNorm);
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

        public void ClearCache()
        {
            foreach (var glyph in cachedGlyphs)
                glyph.IsUploaded = false;

            cachedGlyphs.Clear();
            packer.Clear(cacheTextures[0].ViewWidth, cacheTextures[0].ViewHeight);
        }

        /// <summary>
        /// Upload an RGBA glyph bitmap into the MSDF cache and return its packed sub-rectangle.
        /// </summary>
        public void UploadGlyphBitmap(CommandList commandList, CharacterBitmapRgba bitmap, ref Rectangle subrect, out int bitmapIndex)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if (commandList == null)
                throw new ArgumentNullException(nameof(commandList));

            bitmapIndex = 0;

            var targetSize = new Int2(bitmap.Width, bitmap.Rows);
            if (!packer.Insert(targetSize.X, targetSize.Y, ref subrect))
            {
                RemoveLessUsedGlyphs();
                if (!packer.Insert(targetSize.X, targetSize.Y, ref subrect))
                {
                    // NOTE: same behavior as FontCacheManager today. Multi-page atlases come later.
                    ClearCache();
                    if (!packer.Insert(targetSize.X, targetSize.Y, ref subrect))
                        throw new InvalidOperationException("The rendered glyph is too big for the MSDF cache texture.");
                }
            }

            if (bitmap.Rows != 0 && bitmap.Width != 0)
            {
                var dataBox = new DataBox(bitmap.Buffer, bitmap.Pitch, bitmap.Pitch * bitmap.Rows);
                var region = new ResourceRegion(subrect.Left, subrect.Top, 0, subrect.Right, subrect.Bottom, 1);
                commandList.UpdateSubResource(cacheTextures[0], 0, dataBox, region);
            }

            // Track for eviction behavior parity (frame-based LRU).
            var cached = new MsdfCachedGlyph
            {
                Subrect = subrect,
                BitmapIndex = 0,
                LastUsedFrame = system.FrameCount,
                IsUploaded = true,
            };

            cachedGlyphs.AddFirst(cached.ListNode);
        }

        public void NotifyGlyphUtilization(MsdfCachedGlyph glyph)
        {
            glyph.LastUsedFrame = system.FrameCount;

            if (glyph.ListNode.List != null)
                cachedGlyphs.Remove(glyph.ListNode);

            cachedGlyphs.AddFirst(glyph.ListNode);
        }

        private void RemoveLessUsedGlyphs(int frameCount = 5)
        {
            var limitFrame = system.FrameCount - frameCount;
            var currentNode = cachedGlyphs.Last;

            while (currentNode != null && currentNode.Value.LastUsedFrame < limitFrame)
            {
                currentNode.Value.IsUploaded = false;
                packer.Free(ref currentNode.Value.Subrect);

                var prev = currentNode.Previous;
                cachedGlyphs.RemoveLast();
                currentNode = prev;
            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            foreach (var cacheTexture in cacheTextures)
                cacheTexture.Dispose();

            cacheTextures.Clear();
            cachedGlyphs.Clear();
        }

        /// <summary>
        /// Internal tracking record for eviction parity with the R8 cache.
        /// </summary>
        internal sealed class MsdfCachedGlyph
        {
            public Rectangle Subrect;
            public int BitmapIndex;
            public int LastUsedFrame;
            public bool IsUploaded;

            public readonly LinkedListNode<MsdfCachedGlyph> ListNode;

            public MsdfCachedGlyph()
            {
                ListNode = new LinkedListNode<MsdfCachedGlyph>(this);
            }
        }
    }
}
