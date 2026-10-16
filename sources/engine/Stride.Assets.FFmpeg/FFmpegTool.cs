// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Stride.Assets.FFmpeg;

/// <summary>
/// The ffmpeg command line shipped with this package: locate it, or probe a media file through it.
/// </summary>
public static partial class FFmpegTool
{
    /// <summary>
    /// Full path of the ffmpeg executable for this host, or null when none is shipped for it.
    /// </summary>
    public static string? Locate()
    {
        return ToolLocator.LocateTool("ffmpeg", ensureExecutable: true);
    }

    /// <summary>
    /// Reads the streams and duration of <paramref name="mediaFile"/> from the <c>ffmpeg -i</c> report.
    /// </summary>
    public static FFmpegMediaInfo Probe(string mediaFile)
    {
        var ffmpeg = Locate() ?? throw new FileNotFoundException("ffmpeg was not found.");
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(ffmpeg, $"-hide_banner -i \"{mediaFile}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
        };
        if (!process.Start())
            throw new InvalidOperationException($"Could not start [{ffmpeg}].");

        // The report goes to stderr; ffmpeg exits non-zero because no output is requested
        var stdout = process.StandardOutput.ReadToEndAsync();
        var report = process.StandardError.ReadToEnd();
        process.WaitForExit();
        stdout.Wait();

        var info = ParseReport(report);
        if (info.Streams.Count == 0)
            throw new InvalidOperationException($"ffmpeg found no stream in [{mediaFile}]:\n{report}");
        return info;
    }

    /// <summary>
    /// The streams and duration of <paramref name="mediaFile"/>, or <c>null</c> when ffmpeg is missing or cannot read the file.
    /// </summary>
    public static FFmpegMediaInfo? TryProbe(string mediaFile)
    {
        try
        {
            return Probe(mediaFile);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses the report printed by <c>ffmpeg -i</c>.
    /// </summary>
    public static FFmpegMediaInfo ParseReport(string report)
    {
        var info = new FFmpegMediaInfo();
        foreach (var rawLine in report.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (DurationRegex().Match(line) is { Success: true } duration)
            {
                info.Duration = new TimeSpan(0,
                    int.Parse(duration.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(duration.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(duration.Groups[3].Value, CultureInfo.InvariantCulture),
                    int.Parse(duration.Groups[4].Value, CultureInfo.InvariantCulture) * 10);
                continue;
            }

            if (StreamRegex().Match(line) is not { Success: true } stream)
                continue;

            var details = stream.Groups[3].Value;
            var streamInfo = new FFmpegStreamInfo
            {
                Index = int.Parse(stream.Groups[1].Value, CultureInfo.InvariantCulture),
                Kind = stream.Groups[2].Value switch
                {
                    "Audio" => FFmpegStreamKind.Audio,
                    "Video" => FFmpegStreamKind.Video,
                    "Subtitle" => FFmpegStreamKind.Subtitle,
                    _ => FFmpegStreamKind.Other,
                },
                Codec = details.Split(' ', 2)[0].TrimEnd(','),
                Details = details,
            };
            if (streamInfo.Kind == FFmpegStreamKind.Audio && SampleRateRegex().Match(details) is { Success: true } sampleRate)
                streamInfo.SampleRate = int.Parse(sampleRate.Groups[1].Value, CultureInfo.InvariantCulture);
            if (streamInfo.Kind == FFmpegStreamKind.Video && SizeRegex().Match(details) is { Success: true } size)
            {
                streamInfo.Width = int.Parse(size.Groups[1].Value, CultureInfo.InvariantCulture);
                streamInfo.Height = int.Parse(size.Groups[2].Value, CultureInfo.InvariantCulture);
            }
            info.Streams.Add(streamInfo);
        }
        return info;
    }

    [GeneratedRegex(@"^\s*Duration: (\d+):(\d\d):(\d\d)\.(\d\d)")]
    private static partial Regex DurationRegex();

    // "  Stream #0:1[0x2](und): Audio: aac (LC) (mp4a / 0x6134706D), 44100 Hz, stereo, fltp, 128 kb/s (default)"
    [GeneratedRegex(@"^\s*Stream #\d+:(\d+)(?:\[[^\]]*\])?(?:\([^)]*\))?: (\w+): (.*)$")]
    private static partial Regex StreamRegex();

    [GeneratedRegex(@"(?:^|, )(\d+) Hz")]
    private static partial Regex SampleRateRegex();

    [GeneratedRegex(@"(?:^|, )(\d+)x(\d+)(?:[ ,\[]|$)")]
    private static partial Regex SizeRegex();
}

public sealed class FFmpegMediaInfo
{
    public TimeSpan? Duration { get; set; }

    public List<FFmpegStreamInfo> Streams { get; } = [];
}

public enum FFmpegStreamKind
{
    Audio,
    Video,
    Subtitle,
    Other,
}

public sealed class FFmpegStreamInfo
{
    /// <summary>Stream index inside the container (the N of <c>#0:N</c>).</summary>
    public int Index { get; set; }

    public FFmpegStreamKind Kind { get; set; }

    public string Codec { get; set; } = "";

    /// <summary>The line after the stream kind, as printed by ffmpeg.</summary>
    public string Details { get; set; } = "";

    /// <summary>Audio streams only.</summary>
    public int? SampleRate { get; set; }

    /// <summary>Video streams only.</summary>
    public int? Width { get; set; }

    /// <summary>Video streams only.</summary>
    public int? Height { get; set; }
}
