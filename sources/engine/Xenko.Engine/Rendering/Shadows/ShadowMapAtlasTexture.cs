// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Shadows
{
    /// <summary>
    /// An atlas of shadow maps.
    /// </summary>
    public class ShadowMapAtlasTexture
    {
        private readonly GuillotinePacker packer = new GuillotinePacker();

        private bool clearNeeded = true;

        public ShadowMapAtlasTexture(Texture texture, int textureId)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            Texture = texture;
            packer.Clear(Texture.Width, Texture.Height);
            Width = texture.Width;
            Height = texture.Height;
            Id = textureId;
        }

        public int Id { get; private set; }

        public readonly int Width;

        public readonly int Height;

        public Type FilterType;

        public readonly Texture Texture;

        public void Clear()
        {
            packer.Clear();
        }

        public bool Insert(int width, int height, ref Rectangle bestRectangle)
        {
            return packer.Insert(width, height, ref bestRectangle);
        }

        public bool TryInsert(int width, int height, int count, GuillotinePacker.InsertRectangleCallback inserted)
        {
            return packer.TryInsert(width, height, count, inserted);
        }

        public void MarkClearNeeded()
        {
            clearNeeded = true;
        }

        public void PrepareAsRenderTarget(CommandList commandList)
        {
            // Switch to render target
            commandList.ResourceBarrierTransition(Texture, GraphicsResourceState.DepthWrite);

            if (clearNeeded)
            {
                // TODO GRAPHICS REFACTOR
                commandList.Clear(Texture, DepthStencilClearOptions.DepthBuffer);
                clearNeeded = false;
            }
        }

        public void PrepareAsShaderResourceView(CommandList commandList)
        {
            commandList.ResourceBarrierTransition(Texture, GraphicsResourceState.PixelShaderResource);
        }
    }
}
