// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Stride.Assets.FFmpeg;
using Stride.Audio.Assets;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Video.Assets;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The sound and video importers read what the asset needs from the file through the shipped ffmpeg.
    /// </summary>
    public class TestRawMediaImporters : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-importers-{Guid.NewGuid():N}");

        public TestRawMediaImporters()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void SoundImporterReadsSampleRateAndSplitsTracks()
        {
            var first = WriteSilence("first.wav", 22050);
            var second = WriteSilence("second.wav", 48000);
            var file = RunFFmpeg("stereo.mkv", $"-i \"{first}\" -i \"{second}\" -map 0:a -map 1:a -c:a pcm_s16le");

            var importer = (IRawAssetImporter)new RawSoundAssetImporter();
            var items = importer.Import(file, new UFile("Sounds/Stereo"), new SoundAsset { Spatialized = true }).ToList();

            Assert.Equal(2, items.Count);
            Assert.Equal(["Sounds/Stereo track 0", "Sounds/Stereo track 1"], items.Select(x => x.Location.FullPath));
            var sounds = items.Select(x => (SoundAsset)x.Asset).ToList();
            Assert.Equal([0, 1], sounds.Select(x => x.Index));
            Assert.Equal([22050, 48000], sounds.Select(x => x.SampleRate));
            Assert.All(sounds, x => Assert.True(x.Spatialized));
            Assert.All(sounds, x => Assert.Equal(file, x.Source));
        }

        [Fact]
        public void VideoImporterCoversTheWholeDuration()
        {
            // One second of H.264 at 10 fps
            var file = new UFile(Path.Combine(AppContext.BaseDirectory, "video", "clip.mp4"));

            var importer = (IRawAssetImporter)new RawVideoAssetImporter();
            var item = Assert.Single(importer.Import(file, new UFile("Clip"), new VideoAsset()));

            var video = (VideoAsset)item.Asset;
            Assert.Equal(file, video.Source);
            Assert.Equal(TimeSpan.Zero, video.VideoDuration.StartTime);
            Assert.InRange(video.VideoDuration.EndTime.TotalSeconds, 0.9, 1.1);
        }

        [Fact]
        public void VideoImporterSkipsAFileWithoutVideo()
        {
            var file = new UFile(WriteSilence("sound.wav", 44100));

            var importer = (IRawAssetImporter)new RawVideoAssetImporter();
            Assert.Empty(importer.Import(file, new UFile("Sound"), new VideoAsset()));
        }

        [Fact]
        public void UnreadableFileImportsWithTheDefaults()
        {
            var file = Path.Combine(directory, "broken.wav");
            File.WriteAllText(file, "not a media file");

            var sound = Assert.Single(((IRawAssetImporter)new RawSoundAssetImporter()).Import(file, new UFile("Broken"), new SoundAsset()));
            Assert.Equal(new UFile(file), ((SoundAsset)sound.Asset).Source);
            var video = Assert.Single(((IRawAssetImporter)new RawVideoAssetImporter()).Import(file, new UFile("Broken"), new VideoAsset()));
            Assert.Equal(TimeSpan.MaxValue, ((VideoAsset)video.Asset).VideoDuration.EndTime);
        }

        /// <summary>Half a second of 16-bit mono PCM silence.</summary>
        private string WriteSilence(string fileName, int sampleRate)
        {
            var file = Path.Combine(directory, fileName);
            var dataSize = sampleRate / 2 * 2;
            using var writer = new BinaryWriter(File.Create(file));
            writer.Write("RIFF"u8);
            writer.Write(36 + dataSize);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)1); // channels
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2); // byte rate
            writer.Write((short)2); // block align
            writer.Write((short)16); // bits per sample
            writer.Write("data"u8);
            writer.Write(dataSize);
            writer.Write(new byte[dataSize]);
            return file;
        }

        private UFile RunFFmpeg(string fileName, string arguments)
        {
            var ffmpeg = FFmpegTool.Locate();
            Assert.NotNull(ffmpeg);
            var file = Path.Combine(directory, fileName);
            using var process = Process.Start(new ProcessStartInfo(ffmpeg, $"-hide_banner -loglevel error {arguments} -y \"{file}\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
            var errors = process!.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"ffmpeg failed to generate {fileName}: {errors}");
            return new UFile(file);
        }
    }
}
