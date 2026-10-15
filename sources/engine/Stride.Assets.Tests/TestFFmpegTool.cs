// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using Stride.Assets.FFmpeg;
using Xunit;

namespace Stride.Assets.Tests
{
    public class TestFFmpegTool
    {
        [Fact]
        public void ParsesStreamsAndDurationFromReport()
        {
            const string report = """
                Input #0, mov,mp4,m4a,3gp,3g2,mj2, from 'clip.mp4':
                  Metadata:
                    major_brand     : isom
                  Duration: 00:01:02.34, start: 0.000000, bitrate: 4128 kb/s
                  Stream #0:0[0x1](und): Video: h264 (High) (avc1 / 0x31637661), yuv420p(progressive), 1920x1080 [SAR 1:1 DAR 16:9], 4000 kb/s, 30 fps, 30 tbr, 15360 tbn (default)
                    Metadata:
                      handler_name    : VideoHandler
                  Stream #0:1[0x2](eng): Audio: aac (LC) (mp4a / 0x6134706D), 48000 Hz, stereo, fltp, 128 kb/s (default)
                  Stream #0:2(jpn): Audio: aac (LC) (mp4a / 0x6134706D), 44100 Hz, mono, fltp, 64 kb/s
                  Stream #0:3(eng): Subtitle: mov_text (tx3g / 0x67337874), 0 kb/s
                At least one output file must be specified
                """;

            var info = FFmpegTool.ParseReport(report);

            Assert.Equal(TimeSpan.FromSeconds(62.34), info.Duration);
            Assert.Equal(4, info.Streams.Count);

            var video = info.Streams[0];
            Assert.Equal(0, video.Index);
            Assert.Equal(FFmpegStreamKind.Video, video.Kind);
            Assert.Equal("h264", video.Codec);
            Assert.Equal(1920, video.Width);
            Assert.Equal(1080, video.Height);
            Assert.Null(video.SampleRate);

            var audio = info.Streams.Where(x => x.Kind == FFmpegStreamKind.Audio).ToList();
            Assert.Equal([1, 2], audio.Select(x => x.Index));
            Assert.Equal([48000, 44100], audio.Select(x => x.SampleRate));
            Assert.All(audio, x => Assert.Equal("aac", x.Codec));

            Assert.Equal(FFmpegStreamKind.Subtitle, info.Streams[3].Kind);
        }

        [Fact]
        public void ProbesAWaveFileThroughTheShippedTool()
        {
            Assert.NotNull(FFmpegTool.Locate());

            // Half a second of silence, 22050 Hz mono 16-bit PCM
            const int sampleRate = 22050;
            const int samples = sampleRate / 2;
            var file = Path.Combine(Path.GetTempPath(), $"stride-ffmpeg-probe-{Guid.NewGuid():N}.wav");
            try
            {
                using (var writer = new BinaryWriter(File.Create(file)))
                {
                    var dataSize = samples * 2;
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
                }

                var info = FFmpegTool.Probe(file);

                var stream = Assert.Single(info.Streams);
                Assert.Equal(FFmpegStreamKind.Audio, stream.Kind);
                Assert.Equal(0, stream.Index);
                Assert.Equal(sampleRate, stream.SampleRate);
                Assert.NotNull(info.Duration);
                Assert.InRange(info.Duration.Value.TotalSeconds, 0.45, 0.55);
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}
