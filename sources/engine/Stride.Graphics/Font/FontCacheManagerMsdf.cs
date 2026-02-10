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

        private readonly List<Texture> cacheTextures = [];
        private readonly LinkedList<MsdfCachedGlyph> cachedGlyphs = new();
        private readonly GuillotinePacker packer = new();

        public int AtlasPaddingPixels = 2;

        public IReadOnlyList<Texture> Textures { get; private set; }

        public FontCacheManagerMsdf(FontSystem system, int textureDefaultSize = 2048)
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
            {
                glyph.IsUploaded = false;
                glyph.Owner?.IsBitmapUploaded = false;
            }
                      
            cachedGlyphs.Clear();
            packer.Clear(cacheTextures[0].ViewWidth, cacheTextures[0].ViewHeight);

        }

        /// <summary>
        /// Upload an RGBA glyph bitmap into the MSDF cache and return its packed sub-rectangle.
        /// </summary>
        public MsdfCachedGlyph UploadGlyphBitmap(
            CommandList commandList,
            CharacterSpecification owner,
            CharacterBitmapRgba bitmap,
            ref Rectangle subrect,
            out int bitmapIndex)
        {
            ArgumentNullException.ThrowIfNull(bitmap);
            ArgumentNullException.ThrowIfNull(commandList);

            bitmapIndex = 0;

            var atlasPad = AtlasPaddingPixels;

            if (!packer.Insert(bitmap.Width + atlasPad * 2, bitmap.Rows + atlasPad * 2, ref subrect))
            {
                if (!EnsureSpaceFor(bitmap.Width, bitmap.Rows, atlasPad))
                    throw new InvalidOperationException("MSDF glyph does not fit in cache even after eviction.");

                if (!packer.Insert(bitmap.Width + atlasPad * 2, bitmap.Rows + atlasPad * 2, ref subrect))
                    throw new InvalidOperationException("MSDF cache allocation failed unexpectedly after eviction.");
            }

            if (bitmap.Rows != 0 && bitmap.Width != 0)
            {
                int dstX = subrect.Left + atlasPad;
                int dstY = subrect.Top + atlasPad;

                var dataBox = new DataBox(bitmap.Buffer, bitmap.Pitch, bitmap.Pitch * bitmap.Rows);
                var region = new ResourceRegion(dstX, dstY, 0, dstX + bitmap.Width, dstY + bitmap.Rows, 1);
                commandList.UpdateSubResource(cacheTextures[0], 0, dataBox, region);
            }

            // Track for eviction behavior parity (frame-based LRU).
            var outer = subrect;
            var inner = new Rectangle(outer.Left + atlasPad, outer.Top + atlasPad, bitmap.Width, bitmap.Rows);

            var cached = new MsdfCachedGlyph
            {
                Owner = owner,
                OuterSubrect = outer,
                InnerSubrect = inner,
                BitmapIndex = 0,
                LastUsedFrame = system.FrameCount,
                IsUploaded = true,
            };
            
            cachedGlyphs.AddFirst(cached.ListNode);
            return cached;
        }

        public void NotifyGlyphUtilization(MsdfCachedGlyph glyph)
        {
            glyph.LastUsedFrame = system.FrameCount;

            if (glyph.ListNode.List != null)
                cachedGlyphs.Remove(glyph.ListNode);

            cachedGlyphs.AddFirst(glyph.ListNode);
        }

        private void RemoveLessUsedGlyphs(int frameCount = 1)
        {
            var limitFrame = system.FrameCount - frameCount;
            var currentNode = cachedGlyphs.Last;

            while (currentNode != null && currentNode.Value.LastUsedFrame < limitFrame)
            {
                currentNode.Value.IsUploaded = false;
                currentNode.Value.Owner?.IsBitmapUploaded = false;
                packer.Free(ref currentNode.Value.OuterSubrect);

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
            public int BitmapIndex;
            public int LastUsedFrame;
            public bool IsUploaded;
            public CharacterSpecification Owner;

            public readonly LinkedListNode<MsdfCachedGlyph> ListNode;
            public Rectangle OuterSubrect;
            public Rectangle InnerSubrect;


            public MsdfCachedGlyph()
            {
                ListNode = new LinkedListNode<MsdfCachedGlyph>(this);
            }


        }
        private bool EnsureSpaceFor(int w, int h, int pad)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                RemoveLessUsedGlyphs(pass switch
                {
                    0 => 120,
                    1 => 30,
                    _ => 1,
                });

                var test = new Rectangle();
                if (packer.Insert(w + pad * 2, h + pad * 2, ref test))
                {
                    packer.Free(ref test);
                    return true;
                }
            }

            // FINAL ATTEMPT: If partial eviction failed, wipe the whole cache.
            // This handles high fragmentation or a very "busy" frame.
            
            ClearCache();

            var finalTest = new Rectangle();
            if (packer.Insert(w + pad * 2, h + pad * 2, ref finalTest))
            {
                packer.Free(ref finalTest);
                return true;
            }

            return false;
        }
    }



}
