// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Audio;

namespace Xenko.Assets.Media
{
    /// <summary>
    /// Asset compiler for <see cref="SoundAsset"/>.
    /// </summary>
    [AssetCompiler(typeof(SoundAsset), typeof(AssetCompilationContext))]
    public class SoundAssetCompiler : AssetCompilerBase
    {
        /// <inheritdoc />
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (SoundAsset)assetItem.Asset;
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new DecodeSoundFileCommand(targetUrlInStorage, asset, assetItem.Package));
        }

        protected class DecodeSoundFileCommand : AssetCommand<SoundAsset>
        {
            public DecodeSoundFileCommand(string url, SoundAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
                Version = 1;
            }

            /// <inheritdoc />
            protected override async Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                // Get path to ffmpeg
                var installationDir = DirectoryHelper.GetPackageDirectory("Xenko");
                var binDir = UPath.Combine(installationDir, new UDirectory("Bin"));
                binDir = UPath.Combine(binDir, new UDirectory("Windows"));
                var ffmpeg = UPath.Combine(binDir, new UFile("ffmpeg.exe")).ToWindowsPath();
                if (!File.Exists(ffmpeg))
                {
                    throw new AssetException("Failed to compile a sound asset. ffmpeg was not found.");
                }

                // Get absolute path of asset source on disk
                var assetDirectory = Parameters.Source.GetParent();
                var assetSource = UPath.Combine(assetDirectory, Parameters.Source);

                // Execute ffmpeg to convert source to PCM and then encode with Celt
                var tempFile = Path.GetTempFileName();
                try
                {
                    var channels = Parameters.Spatialized ? 1 : 2;
                    var commandLine = "  -hide_banner -loglevel error" + // hide most log output
                                      $" -i \"{assetSource.ToWindowsPath()}\"" + // input file
                                      $" -f f32le -acodec pcm_f32le -ac {channels} -ar {Parameters.SampleRate}" + // codec
                                      $" -map 0:{Parameters.Index}" + // stream index
                                      $" -y \"{tempFile}\""; // output file (always overwrite)
                    var ret = await ShellHelper.RunProcessAndGetOutputAsync(ffmpeg, commandLine, commandContext.Logger);
                    if (ret != 0 || commandContext.Logger.HasErrors)
                    {
                        throw new AssetException($"Failed to compile a sound asset, ffmpeg failed to convert {assetSource}");
                    }

                    var encoder = new Celt(Parameters.SampleRate, CompressedSoundSource.SamplesPerFrame, channels, false);

                    var uncompressed = CompressedSoundSource.SamplesPerFrame * channels * sizeof(short); //compare with int16 for CD quality comparison.. but remember we are dealing with 32 bit floats for encoding!!
                    var target = (int)Math.Floor(uncompressed / (float)Parameters.CompressionRatio);

                    var dataUrl = Url + "_Data";
                    var newSound = new Sound
                    {
                        CompressedDataUrl = dataUrl,
                        Channels = channels,
                        SampleRate = Parameters.SampleRate,
                        StreamFromDisk = Parameters.StreamFromDisk,
                        Spatialized = Parameters.Spatialized,
                    };

                    //make sure we don't compress celt data
                    commandContext.AddTag(new ObjectUrl(UrlType.Content, dataUrl), Builder.DoNotCompressTag);

                    var frameSize = CompressedSoundSource.SamplesPerFrame * channels;
                    using (var reader = new BinaryReader(new FileStream(tempFile, FileMode.Open, FileAccess.Read)))
                    using (var outputStream = MicrothreadLocalDatabases.DatabaseFileProvider.OpenStream(dataUrl, VirtualFileMode.Create, VirtualFileAccess.Write, VirtualFileShare.Read, StreamFlags.Seekable))
                    {
                        var writer = new BinarySerializationWriter(outputStream);

                        var outputBuffer = new byte[target];
                        var buffer = new float[frameSize];
                        var count = 0;
                        var length = reader.BaseStream.Length; // Cache the length, because this getter is expensive to use
                        for (var position = 0; position < length; position += sizeof(float))
                        {
                            if (count == frameSize) //flush
                            {
                                var len = encoder.Encode(buffer, outputBuffer);
                                writer.Write((short)len);
                                outputStream.Write(outputBuffer, 0, len);

                                newSound.Samples += count / channels;
                                newSound.NumberOfPackets++;
                                newSound.MaxPacketLength = Math.Max(newSound.MaxPacketLength, len);

                                count = 0;
                                Array.Clear(buffer, 0, frameSize);
                            }

                            buffer[count] = reader.ReadSingle();
                            count++;
                        }

                        if (count > 0) //flush
                        {
                            var len = encoder.Encode(buffer, outputBuffer);
                            writer.Write((short)len);
                            outputStream.Write(outputBuffer, 0, len);

                            newSound.Samples += count / channels;
                            newSound.NumberOfPackets++;
                            newSound.MaxPacketLength = Math.Max(newSound.MaxPacketLength, len);
                        }
                    }

                    var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                    assetManager.Save(Url, newSound);

                    return ResultStatus.Successful;
                }
                finally
                {
                    File.Delete(tempFile);
                }
            }
        }
    }

    [DataContract]
    public sealed class DecodeSoundFileParameters
    {
        public int StreamIndex;

        public Asset SoundAsset;

        public DecodeSoundFileParameters()
        {

        }

        public DecodeSoundFileParameters(Asset soundAsset, int streamIndex = 0)
        {
            SoundAsset = soundAsset;
            StreamIndex = streamIndex;
        }
    }
}
