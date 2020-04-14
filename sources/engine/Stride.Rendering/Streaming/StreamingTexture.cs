// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Streaming;
using Stride.Graphics;

namespace Stride.Streaming
{
    /// <summary>
    /// Texture streaming object.
    /// </summary>
    public class StreamingTexture : StreamableResource
    {
        /// <summary>
        /// The texture to stream.
        /// </summary>
        protected Texture texture;

        /// <summary>
        /// The texture to synchronize. Created and prepared in a background to be swaped with the streamed texture. See <see cref="FlushSync"/>.
        /// </summary>
        protected Texture textureToSync;

        /// <summary>
        /// The actual texture description.
        /// </summary>
        protected ImageDescription description;

        /// <summary>
        /// The amount of the resident mips (uploaded to the GPU).
        /// </summary>
        protected int residentMips;

        /// <summary>
        /// The texture cached mip maps infos.
        /// </summary>
        protected MipInfo[] mipInfos;

        /// <summary>
        /// Helper structure used to pre-cache <see cref="StreamingTexture"/> mip maps metadata. Used to improve streaming performance (smaller CPU usage).
        /// </summary>
        protected struct MipInfo
        {
            public int Width;
            public int Height;
            public int RowPitch;
            public int SlicePitch;
            public int TotalSize; // SlicePitch * ArraySize

            public MipInfo(int width, int height, int rowPitch, int slicePitch, int arraySize)
            {
                Width = width;
                Height = height;
                RowPitch = rowPitch;
                SlicePitch = slicePitch;
                TotalSize = slicePitch * arraySize;
            }
        }

        internal StreamingTexture(StreamingManager manager, [NotNull] Texture texture)
            : base(manager)
        {
            this.texture = texture;
            this.DisposeBy(this.texture);
            residentMips = 0;
        }

        /// <summary>
        /// Gets the texture object.
        /// </summary>
        public Texture Texture => texture;

        /// <summary>
        /// Gets the texture image description (available in the storage container).
        /// </summary>
        public ImageDescription Description => description;

        /// <summary>
        /// Gets the total amount of mip levels.
        /// </summary>
        public int TotalMipLevels => description.MipLevels;

        /// <summary>
        /// Gets the width of maximum texture mip.
        /// </summary>
        public int TotalWidth => description.Width;

        /// <summary>
        /// Gets the height of maximum texture mip.
        /// </summary>
        public int TotalHeight => description.Height;

        /// <summary>
        /// Gets the number of textures in an array.
        /// </summary>
        public int ArraySize => description.ArraySize;

        /// <summary>
        /// Gets a value indicating whether this texture is a cube map.
        /// </summary>
        /// <value><c>true</c> if this texture is a cube map; otherwise, <c>false</c>.</value>
        public bool IsCubeMap => description.Dimension == TextureDimension.TextureCube;

        /// <summary>
        /// Gets the texture texels format
        /// </summary>
        public PixelFormat Format => description.Format;

        /// <summary>
        /// Gets index of the highest resident mip map (may be equal to MipLevels if no mip has been uploaded). Note: mip=0 is the highest (top quality)
        /// </summary>
        /// <returns>Mip index</returns>
        public int HighestResidentMipIndex => TotalMipLevels - residentMips;

        /// <inheritdoc />
        public override object Resource => texture;

        /// <inheritdoc />
        public override int CurrentResidency => residentMips;

        /// <inheritdoc />
        public override int AllocatedResidency => Texture.MipLevels;

        /// <inheritdoc />
        public override int MaxResidency => description.MipLevels;

        /// <inheritdoc />
        public override int CalculateTargetResidency(StreamingQuality quality)
        {
            if (MathUtil.IsZero(quality))
                return 0;

            var result = Math.Max(1, (int)(TotalMipLevels * quality));

            // Compressed formats have aligment restrictions on the dimensions of the texture (minimum size must be 4)
            if (Format.IsCompressed() && TotalMipLevels >= 3)
                result = MathUtil.Clamp(result, 3, TotalMipLevels);

            return result;
        }

        /// <inheritdoc />
        public override int CalculateRequestedResidency(int targetResidency)
        {
            int requestedResidency;

            // Check if need to increase it's residency or decrease
            if (targetResidency > CurrentResidency)
            {
                // Stream target quality in steps but lower mips at once
                requestedResidency = Math.Min(targetResidency, Math.Max(CurrentResidency + 2, 5));

                // Stream target quality in steps
                //requestedResidency = currentResidency + 1;

                // Stream target quality at once
                //requestedResidency = targetResidency;
            }
            else
            {
                // Stream target quality in steps
                //requestedResidency = currentResidency - 1;

                // Stream target quality at once
                requestedResidency = targetResidency;
            }

            return requestedResidency;
        }

        /// <inheritdoc />
        internal override bool CanBeUpdated => textureToSync == null && base.CanBeUpdated;

        internal void Init(IDatabaseFileProviderService databaseFileProviderService, [NotNull] ContentStorage storage, ref ImageDescription imageDescription)
        {
            if (imageDescription.Depth != 1)
                throw new ContentStreamingException("Texture streaming supports only 2D textures and 2D texture arrays.", storage);

            Init(databaseFileProviderService, storage);
            texture.FullQualitySize = new Size3(imageDescription.Width, imageDescription.Height, imageDescription.Depth);
            description = imageDescription;
            residentMips = 0;
            CacheMipMaps();
        }

        /// <inheritdoc />
        internal override void FlushSync()
        {
            if (textureToSync == null)
                return;

            // register the new memory usage
            Manager.RegisterMemoryUsage(textureToSync.SizeInBytes - texture.SizeInBytes);

            // Texture is loaded and created in the async task.
            // But we have to sync on main therad with the engine to prevent leaks.
            // Here we internaly swap two textures data (texture with textureToSync).

            texture.Swap(textureToSync);
#if DEBUG
            texture.Name = Storage.Url;
#endif

            textureToSync.Dispose();
            textureToSync = null;
        }

        /// <inheritdoc />
        internal override void Release()
        {
            // Unlink from the texture
            this.RemoveDisposeBy(Texture);

            base.Release();
        }

        /// <inheritdoc />
        protected override void Destroy()
        {
            StopStreaming();

            // register the change of memory usage
            Manager.RegisterMemoryUsage(-Texture.SizeInBytes);
            
            if (textureToSync != null)
            {
                textureToSync.Dispose();
                textureToSync = null;
            }

            base.Destroy();
        }

        /// <inheritdoc />
        protected override Task StreamAsync(int residency)
        {
            return new Task(() => StreamingTask(residency), cancellationToken.Token);
        }

        private void CacheMipMaps()
        {
            var mipLevels = TotalMipLevels;
            mipInfos = new MipInfo[mipLevels];
            var isBlockCompressed =
                (Format >= PixelFormat.BC1_Typeless && Format <= PixelFormat.BC5_SNorm) ||
                (Format >= PixelFormat.BC6H_Typeless && Format <= PixelFormat.BC7_UNorm_SRgb);

            for (var mipIndex = 0; mipIndex < mipLevels; mipIndex++)
            {
                GetMipSize(isBlockCompressed, mipIndex, out int mipWidth, out int mipHeight);

                Image.ComputePitch(Format, mipWidth, mipHeight, out int rowPitch, out int slicePitch, out int _, out int _);

                mipInfos[mipIndex] = new MipInfo(mipWidth, mipHeight, rowPitch, slicePitch, ArraySize);
            }
        }

        private void GetMipSize(bool isBlockCompressed, int mipIndex, out int width, out int height)
        {
            width = Math.Max(1, TotalWidth >> mipIndex);
            height = Math.Max(1, TotalHeight >> mipIndex);

            if (isBlockCompressed && ((width % 4) != 0 || (height % 4) != 0))
            {
                width = unchecked((int)(((uint)(width + 3)) & ~3U));
                height = unchecked((int)(((uint)(height + 3)) & ~3U));
            }
        }

        private void StreamingTask(int residency)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // Cache data
            var mipsChange = residency - CurrentResidency;
            var mipsCount = residency;
            Debug.Assert(mipsChange != 0, $"mipsChange[{mipsChange}] != 0");

            if (residency == 0)
            {
                // Release
                Manager.RegisterMemoryUsage(-texture.SizeInBytes);
                texture.ReleaseData();
                residentMips = 0;
                return;
            }

            try
            {
                Storage.LockChunks();

                // Setup texture description
                TextureDescription newDesc = description;
                var newHighestResidentMipIndex = TotalMipLevels - mipsCount;
                newDesc.MipLevels = mipsCount;
                var topMip = mipInfos[description.MipLevels - newDesc.MipLevels];
                newDesc.Width = topMip.Width;
                newDesc.Height = topMip.Height;

                // Load chunks
                var mipsData = new IntPtr[mipsCount];
                for (var mipIndex = 0; mipIndex < mipsCount; mipIndex++)
                {
                    var totalMipIndex = newHighestResidentMipIndex + mipIndex;
                    var chunk = Storage.GetChunk(totalMipIndex);
                    if (chunk == null)
                        throw new ContentStreamingException("Data chunk is missing.", Storage);

                    if (chunk.Size != mipInfos[totalMipIndex].TotalSize)
                        throw new ContentStreamingException("Data chunk has invalid size.", Storage);

                    var data = chunk.GetData(fileProvider);
                    if (!chunk.IsLoaded)
                        throw new ContentStreamingException("Data chunk is not loaded.", Storage);

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    mipsData[mipIndex] = data;
                }

                // Get data boxes
                var dataBoxIndex = 0;
                var dataBoxes = new DataBox[newDesc.MipLevels * newDesc.ArraySize];
                for (var arrayIndex = 0; arrayIndex < newDesc.ArraySize; arrayIndex++)
                {
                    for (var mipIndex = 0; mipIndex < mipsCount; mipIndex++)
                    {
                        var totalMipIndex = newHighestResidentMipIndex + mipIndex;
                        var info = mipInfos[totalMipIndex];

                        dataBoxes[dataBoxIndex].DataPointer = mipsData[mipIndex] + info.SlicePitch * arrayIndex;
                        dataBoxes[dataBoxIndex].RowPitch = info.RowPitch;
                        dataBoxes[dataBoxIndex].SlicePitch = info.SlicePitch;
                        dataBoxIndex++;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                // Create texture (use staging object and swap it on sync)
                textureToSync = Texture.New(texture.GraphicsDevice, newDesc, new TextureViewDescription(), dataBoxes);
                textureToSync.FullQualitySize = texture.FullQualitySize;

                residentMips = newDesc.MipLevels;
            }
            finally
            {
                Storage.UnlockChunks();
            }
        }
    }
}
