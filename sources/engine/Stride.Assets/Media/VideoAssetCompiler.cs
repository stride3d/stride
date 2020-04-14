// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using System.Linq;
using Xenko.Core;

using Xenko.Video.FFmpeg;
using FFmpeg.AutoGen;

namespace Xenko.Assets.Media
{
    /// <summary>
    /// Asset compiler for <see cref="VideoAsset"/>.
    /// </summary>
    [AssetCompiler(typeof(VideoAsset), typeof(AssetCompilationContext))]
    public class VideoAssetCompiler : AssetCompilerBase
    {
        /// <inheritdoc />
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(SoundAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
        }

        /// <inheritdoc />
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            VideoAsset asset = (VideoAsset)assetItem.Asset;

            AVCodecID[] listSupportedCodecNames = null;
            switch (context.Platform)
            {
                case Core.PlatformType.Android:
                    {
                        listSupportedCodecNames = new []
                        {
                           AVCodecID.AV_CODEC_ID_H264
                        };
                        break;
                    }
            }

            VideoConvertParameters parameter = new VideoConvertParameters(GetAbsolutePath(assetItem, asset.Source), asset, context.Platform);
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new EncodeVideoFileCommand(targetUrlInStorage, parameter, assetItem.Package, listSupportedCodecNames));
        }

        private class EncodeVideoFileCommand : AssetCommand<VideoConvertParameters>
        {
            //List of supported codec name (or null if all codec are supported)
            //If a video asset format is not supported (does not belong to the list), the AssetCompiler will reencode the video into a supported format
            AVCodecID[] ListSupportedCodecNames;

            public EncodeVideoFileCommand(string url, VideoConvertParameters description, IAssetFinder assetFinder, AVCodecID[] listSupportedCodecNames)
                : base(url, description, assetFinder)
            {
                Version = 4;
                ListSupportedCodecNames = listSupportedCodecNames;
            }

            public override IEnumerable<ObjectUrl> GetInputFiles()
            {
                yield return new ObjectUrl(UrlType.File, Parameters.SourcePathFromDisk);
            }

            /// <inheritdoc />
            protected override async Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                VideoAsset videoAsset = Parameters.Video;

                try
                {
                    // Get path to ffmpeg
                    var ffmpeg = ToolLocator.LocateTool("ffmpeg.exe")?.ToWindowsPath() ?? throw new AssetException("Failed to compile a video asset, ffmpeg was not found.");

                    // Get absolute path of asset source on disk
                    var assetDirectory = videoAsset.Source.GetParent();
                    var assetSource = UPath.Combine(assetDirectory, videoAsset.Source);

                    //=====================================================================================
                    //Get the info from the video codec
                    
                    //Check if we need to reencode the video
                    var mustReEncodeVideo = false;
                    var sidedataStripCommand = "";

                    // check that the video file format is supported
                    if (Parameters.Platform == PlatformType.Windows && videoAsset.Source.GetFileExtension() != ".mp4")
                        mustReEncodeVideo = true;

                    //Use FFmpegMedia object (need to check more details first before I can use it)
                    VideoStream videoStream = null;
                    AudioStream audioStream = null;
                    FFmpegUtils.PreloadLibraries();
                    FFmpegUtils.Initialize();
                    using (var media = new FFmpegMedia())
                    {
                        media.Open(assetSource.ToWindowsPath());

                        // Get the first video stream
                        videoStream = media.Streams.OfType<VideoStream>().FirstOrDefault();
                        if (videoStream == null)
                            throw new AssetException("Failed to compile a video asset. Did not find the VideoStream from the media.");

                        // On windows MediaEngineEx player only decode the first video if the video is detected as a stereoscopic video, 
                        // so we remove the tags inside the video in order to ensure the same behavior as on other platforms (side by side decoded texture)
                        // Unfortunately it does seem possible to disable this behavior from the MediaEngineEx API.
                        if (Parameters.Platform == PlatformType.Windows && media.IsStereoscopicVideo(videoStream))
                        {
                            mustReEncodeVideo = true;
                            sidedataStripCommand = "-vf sidedata=delete";
                        }

                        // Get the first audio stream
                        audioStream = media.Streams.OfType<AudioStream>().FirstOrDefault();
                    }
                    Size2 videoSize = new Size2(videoStream.Width, videoStream.Height);

                    //check the format
                    if (ListSupportedCodecNames != null)
                    {
                        if (Array.IndexOf(ListSupportedCodecNames, videoStream.Codec) < 0) mustReEncodeVideo = true;
                    }

                    // check if video need to be trimmed
                    var videoDuration = videoAsset.VideoDuration;
                    if (videoDuration.Enabled && (videoDuration.StartTime != TimeSpan.Zero ||
                        videoDuration.EndTime.TotalSeconds < videoStream.Duration.TotalSeconds - MathUtil.ZeroToleranceDouble))
                        mustReEncodeVideo = true;

                    //check the video target and source resolution
                    Size2 targetSize;
                    if (videoAsset.IsSizeInPercentage)
                        targetSize = new Size2((int)(videoSize.Width * videoAsset.Width / 100.0f), (int)(videoSize.Height * videoAsset.Height / 100.0f));
                    else
                        targetSize = new Size2((int)(videoAsset.Width), (int)(videoAsset.Height));

                    // ensure that the size is a multiple of 2 (ffmpeg cannot output video not multiple of 2, at least with this codec)
                    if (targetSize.Width % 2 == 1)
                        targetSize.Width += 1;
                    if (targetSize.Height % 2 == 1)
                        targetSize.Height += 1;

                    if (targetSize.Width != videoSize.Width || targetSize.Height != videoSize.Height) mustReEncodeVideo = true;

                    //check the audio settings
                    int audioChannelsTarget = audioStream == null? 0: audioStream.ChannelCount;
                    bool mustReEncodeAudioChannels = false;
                    if (videoAsset.IsAudioChannelMono)
                    {
                        audioChannelsTarget = 1;
                        if (audioStream != null && audioStream.ChannelCount != audioChannelsTarget)
                        {
                            mustReEncodeAudioChannels = true;
                            mustReEncodeVideo = true;
                        }
                    }

                    // Execute ffmpeg to convert source to H.264
                    string tempFile = null;
                    try
                    {
                        if (mustReEncodeVideo)
                        {
                            string targetCodecFormat = "h264";  //hardcodec for now
                            commandContext.Logger.Info(string.Format("Video Asset Compiler: \"{0}\". Re-encode the Video. Format:{1}, Size:{2}x{3}. Audio Channels:{4}",
                                videoAsset.Source.GetFileName(), targetCodecFormat, targetSize.Width, targetSize.Height, audioChannelsTarget));

                            tempFile = Path.GetTempFileName();
                            string channelFlag = "";
                            if (mustReEncodeAudioChannels)
                            {
                                channelFlag = string.Format(" -ac {0}", audioChannelsTarget);
                            }

                            var startTime = videoDuration.StartTime;
                            var duration = videoDuration.EndTime - videoDuration.StartTime;
                            var trimmingOptions = videoDuration.Enabled ?
                                    $" -ss {startTime.Hours:D2}:{startTime.Minutes:D2}:{startTime.Seconds:D2}.{startTime.Milliseconds:D3}" +
                                    $" -t {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}.{duration.Milliseconds:D3}": 
                                    "";

                            var commandLine = "  -hide_banner -loglevel error" + // hide most log output
                                              "  -nostdin" + // no interaction (background process)
                                              $" -i \"{assetSource.ToWindowsPath()}\"" + // input file
                                              $"{trimmingOptions}" + 
                                              "  -f mp4 -vcodec " + targetCodecFormat + // codec
                                              channelFlag + // audio channels
                                              $"  -vf scale={targetSize.Width}:{targetSize.Height} " + // adjust the resolution
                                              sidedataStripCommand +   // strip of stereoscopic sidedata tag
                                              //" -an" + // no audio
                                              //" -pix_fmt yuv422p" + // pixel format (planar YUV 4:2:2, 16bpp, (1 Cr & Cb sample per 2x1 Y samples))
                                              $" -y \"{tempFile}\""; // output file (always overwrite)
                            var ret = await ShellHelper.RunProcessAndGetOutputAsync(ffmpeg, commandLine, commandContext.Logger);
                            if (ret != 0 || commandContext.Logger.HasErrors)
                                throw new AssetException($"Failed to compile a video asset. ffmpeg failed to convert {assetSource}.");
                        }
                        else
                        {
                            commandContext.Logger.Info(string.Format("Video Asset Compiler: \"{0}\". No Re-encoding necessary",
                                videoAsset.Source.GetFileName()));

                            // Use temporary file
                            tempFile = assetSource.ToWindowsPath();
                        }

                        var dataUrl = Url + "_Data";
                        var video = new Video.Video
                        {
                            CompressedDataUrl = dataUrl,
                        };

                        // Make sure we don't compress h264 data
                        commandContext.AddTag(new ObjectUrl(UrlType.Content, dataUrl), Builder.DoNotCompressTag);

                        // Write the data
                        using (var reader = new BinaryReader(new FileStream(tempFile, FileMode.Open, FileAccess.Read)))
                        using (var outputStream = MicrothreadLocalDatabases.DatabaseFileProvider.OpenStream(dataUrl, VirtualFileMode.Create, VirtualFileAccess.Write, VirtualFileShare.Read, StreamFlags.Seekable))
                        {
                            // For now write everything at once, 1MB at a time
                            var length = reader.BaseStream.Length;
                            for (var position = 0L; position < length; position += 2 << 20)
                            {
                                var buffer = reader.ReadBytes(2 << 20);
                                outputStream.Write(buffer, 0, buffer.Length);
                            }
                        }

                        var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                        assetManager.Save(Url, video);

                        return ResultStatus.Successful;
                    }
                    finally
                    {
                        if (mustReEncodeVideo)
                        {
                            if (tempFile != null) File.Delete(tempFile);
                        }
                    }
                }
                catch (AssetException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new AssetException("Failed to compile a video asset. Unexpected exception.", ex);
                }
            }
        }
    }

    /// <summary>
    /// SharedParameters used for converting/processing the video in the storage.
    /// </summary>
    [DataContract]
    public class VideoConvertParameters
    {
        public VideoConvertParameters()
        {
        }

        public VideoConvertParameters(
            UFile sourcePathFromDisk,
            VideoAsset video,
            PlatformType platform)
        {
            SourcePathFromDisk = sourcePathFromDisk;
            Video = video;
            Platform = platform;
        }

        public UFile SourcePathFromDisk;

        public VideoAsset Video;

        public PlatformType Platform;
    }
}
