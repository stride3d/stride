// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Editor.Resources;
using Stride.Graphics;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// The context used when building the thumbnail of an asset in a Package.
    /// </summary>
    public class ThumbnailCompilerContext : AssetCompilerContext
    {
        private readonly object thumbnailCounterLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ThumbnailCompilerContext"/> class.
        /// </summary>
        public ThumbnailCompilerContext()
        {
            ThumbnailResolution = 128 * Int2.One;
            CompilationContext = typeof(ThumbnailCompilationContext);
        }

        /// <summary>
        /// Gets the desired resolution for thumbnails.
        /// </summary>
        public Int2 ThumbnailResolution { get; private set; }

        /// <summary>
        /// Indicate whether the fact that a thumbnail has been built should be notified with <see cref="NotifyThumbnailBuilt"/>
        /// </summary>
        /// <remarks>Use this property to avoid doing unnecessary stream operation when <see cref="ThumbnailBuilt"/> event has no subscriber.</remarks>
        public bool ShouldNotifyThumbnailBuilt => ThumbnailBuilt != null;

        /// <summary>
        /// The array of data representing the thumbnail to display when a thumbnail build failed.
        /// </summary>
        public static Task<byte[]> BuildFailedThumbnail = Task.Run(() => HandleBrokenThumbnail());

        /// <summary>
        /// The event raised when a thumbnail has finished to build.
        /// </summary>
        public event EventHandler<ThumbnailBuiltEventArgs> ThumbnailBuilt;

        /// <summary>
        /// Notify that the thumbnail has just been built. This method will raise the <see cref="ThumbnailBuilt"/> event.
        /// </summary>
        /// <param name="assetItem">The asset item whose thumbnail has been built.</param>
        /// <param name="result">A <see cref="ThumbnailBuildResult"/> value indicating whether the build was successful, failed or cancelled.</param>
        /// <param name="changed">A boolean indicating whether the thumbnal has changed since the last generation.</param>
        /// <param name="thumbnailStream">A stream to the thumbnail image.</param>
        /// <param name="thumbnailHash"></param>
        internal void NotifyThumbnailBuilt(AssetItem assetItem, ThumbnailBuildResult result, bool changed, Stream thumbnailStream, ObjectId thumbnailHash)
        {
            try
            {
                // TODO: this lock seems to be useless now, check if we can safely remove it
                Monitor.Enter(thumbnailCounterLock);
                var handler = ThumbnailBuilt;
                if (handler != null)
                {
                    // create the thumbnail build event arguments
                    var thumbnailBuiltArgs = new ThumbnailBuiltEventArgs
                    {
                        AssetId = assetItem.Id,
                        Url = assetItem.Location,
                        Result = result,
                        ThumbnailChanged = changed
                    };

                    Monitor.Exit(thumbnailCounterLock);
                    
                    // Open the image data stream if the build succeeded
                    if (thumbnailBuiltArgs.Result == ThumbnailBuildResult.Succeeded)
                    {
                        thumbnailBuiltArgs.ThumbnailStream = thumbnailStream;
                        thumbnailBuiltArgs.ThumbnailId = thumbnailHash;
                    }
                    else if (BuildFailedThumbnail != null)
                    {
                        thumbnailBuiltArgs.ThumbnailStream = new MemoryStream(BuildFailedThumbnail.Result);
                    }
                    handler(assetItem, thumbnailBuiltArgs);
                }
            }
            finally
            {
                if (Monitor.IsEntered(thumbnailCounterLock))
                    Monitor.Exit(thumbnailCounterLock);
            }
        }

        private static byte[] HandleBrokenThumbnail()
        {
            // Load broken asset thumbnail
            var assetBrokenThumbnail = Image.Load(DefaultThumbnails.AssetBrokenThumbnail);

            // Apply thumbnail status in corner
            ThumbnailBuildHelper.ApplyThumbnailStatus(assetBrokenThumbnail, LogMessageType.Error);
            var memoryStream = new MemoryStream();
            assetBrokenThumbnail.Save(memoryStream, ImageFileType.Png);

            return memoryStream.ToArray();
        }
    }
}
