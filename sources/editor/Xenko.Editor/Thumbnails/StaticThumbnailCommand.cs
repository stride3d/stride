// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.TextureConverter;
using Xenko.Graphics;

namespace Xenko.Editor.Thumbnails
{
    /// <summary>
    /// Command used to build a thumbnail from a static image.
    /// </summary>
    public class StaticThumbnailCommand<T> : AssetCommand<StaticThumbnailCommandParameters>, IThumbnailCommand
    {
        private readonly byte[] staticImageData;

        private readonly Int2 thumbnailSize;

        // TODO: This is not serializable (OK for now since thumbnails are never built in a separate process); later, a specific class to store results will be needed
        /// <inheritdoc />
        public LogMessageType DependencyBuildStatus { get; set; }

        public StaticThumbnailCommand(string url, byte[] staticImageData, Int2 thumbnailSize, bool loadAsSRgb, IAssetFinder assetFinder)
            : base(url, new StaticThumbnailCommandParameters(thumbnailSize, typeof(T).FullName, loadAsSRgb), assetFinder)
        {
            this.staticImageData = staticImageData;
            this.thumbnailSize = thumbnailSize;
        }

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);
            if (DependencyBuildStatus >= LogMessageType.Warning)
                writer.Write(DependencyBuildStatus);
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            // load the sound thumbnail image from the resources
            using (var imageStream = new MemoryStream(staticImageData))
            using (var image = Image.Load(imageStream))
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(image, Parameters.SRgb))
            {
                // Rescale image so that it fits the thumbnail asked resolution
                texTool.Decompress(texImage, texImage.Format.IsSRgb());
                texTool.Resize(texImage, thumbnailSize.X, thumbnailSize.Y, Filter.Rescaling.Lanczos3);

                // Save
                using (var outputImageStream = ContentManager.FileProvider.OpenStream(Url, VirtualFileMode.Create, VirtualFileAccess.Write))
                using (var outputImage = texTool.ConvertToXenkoImage(texImage))
                {
                    ThumbnailBuildHelper.ApplyThumbnailStatus(outputImage, DependencyBuildStatus);

                    outputImage.Save(outputImageStream, ImageFileType.Png);

                    commandContext.Logger.Verbose($"Thumbnail creation successful [{Url}] to ({outputImage.Description.Width}x{outputImage.Description.Height},{outputImage.Description.Format})");
                }
            }

            return Task.FromResult(ResultStatus.Successful);
        }
    }
        
    /// <summary>
    /// The parameters of the animation thumbnail command that will be used to produce the command hash.
    /// Since the animation image is constant, only the size of the thumbnail should be hashed.
    /// </summary>
    [DataContract]
    public class StaticThumbnailCommandParameters
    {
        public Int2 ThumbnailSize;

        public string Typename;

        public bool SRgb;

        public StaticThumbnailCommandParameters()
        {
        }

        public StaticThumbnailCommandParameters(Int2 thumbnailSize, string typename, bool srgb)
        {
            ThumbnailSize = thumbnailSize;
            Typename = typename;
            SRgb = srgb;
        }
    }
}
