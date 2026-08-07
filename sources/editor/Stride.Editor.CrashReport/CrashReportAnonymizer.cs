// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Stride.Editor.CrashReport;

/// <summary>
/// Strips the user name and profile path from crash report text before it leaves the machine.
/// It also makes paths easier to copy and paste between machines.
/// </summary>
public static class CrashReportAnonymizer
{
    public static void Scrub(CrashReportData report)
    {
        for (var i = 0; i < report.Data.Count; i++)
        {
            report.Data[i] = (report.Data[i].Item1, Scrub(report.Data[i].Item2));
        }
    }

    public static string Scrub(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string.IsNullOrEmpty(userProfile))
            text = Regex.Replace(text, Regex.Escape(userProfile), "%USERPROFILE%", RegexOptions.IgnoreCase);

        var userName = Environment.GetEnvironmentVariable("USERNAME");
        if (!string.IsNullOrEmpty(userName))
            text = Regex.Replace(text, $@"\b{Regex.Escape(userName)}\b", "%USERNAME%", RegexOptions.IgnoreCase);

        return text;
    }

    /// <summary>
    /// Masks the profile path and user name inside a region of a binary buffer (e.g. minidump strings and
    /// memory ranges), in both ASCII and UTF-16. Binary offsets cannot shift, so matches are overwritten
    /// in place with same-length 'x' runs.
    /// </summary>
    public static void Scrub(byte[] buffer, int offset, int length)
    {
        Scrub(buffer, offset, length,
            Environment.GetEnvironmentVariable("USERPROFILE"),
            Environment.GetEnvironmentVariable("USERNAME"));
    }

    public static void Scrub(byte[] buffer, int offset, int length, string userProfile, string userName)
    {
        if (!string.IsNullOrEmpty(userProfile))
            MaskAllEncodings(buffer, offset, length, userProfile, requireBoundary: false);
        if (!string.IsNullOrEmpty(userName))
            MaskAllEncodings(buffer, offset, length, userName, requireBoundary: true);
    }

    private static void MaskAllEncodings(byte[] buffer, int offset, int length, string text, bool requireBoundary)
    {
        if (text.All(char.IsAscii))
        {
            Mask(buffer, offset, length, text, 1, requireBoundary);
            Mask(buffer, offset, length, text, 2, requireBoundary);
            return;
        }

        // Non-ASCII: per-character case folding does not survive multi-byte encodings, so match the exact
        // byte patterns of the realistic casings in each encoding instead
        foreach (var variant in new[] { text, text.ToLowerInvariant(), text.ToUpperInvariant() }.Distinct())
        {
            MaskPattern(buffer, offset, length, Encoding.Unicode.GetBytes(variant), 2, requireBoundary);
            MaskPattern(buffer, offset, length, Encoding.UTF8.GetBytes(variant), 1, requireBoundary);
            if (variant.All(c => c <= 0xFF))
                MaskPattern(buffer, offset, length, Encoding.Latin1.GetBytes(variant), 1, requireBoundary);
        }
    }

    private static void MaskPattern(byte[] buffer, int offset, int length, byte[] pattern, int stride, bool requireBoundary)
    {
        var end = Math.Min(offset + length, buffer.Length);
        for (var i = Math.Max(offset, 0); i + pattern.Length <= end; i++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length && match; j++)
                match = buffer[i + j] == pattern[j];
            if (!match)
                continue;
            if (requireBoundary && (IsWordChar(buffer, i - stride, stride) || IsWordChar(buffer, i + pattern.Length, stride)))
                continue;

            for (var j = 0; j < pattern.Length; j++)
                buffer[i + j] = (byte)(stride == 2 && j % 2 == 1 ? 0 : 'x');
            i += pattern.Length - 1;
        }
    }

    /// <summary>Case-insensitive search and mask at the given character stride (1 = ASCII, 2 = UTF-16).</summary>
    private static void Mask(byte[] buffer, int offset, int length, string text, int stride, bool requireBoundary)
    {
        var end = Math.Min(offset + length, buffer.Length);
        for (var i = Math.Max(offset, 0); i + text.Length * stride <= end; i++)
        {
            var match = true;
            for (var j = 0; j < text.Length && match; j++)
            {
                var c = (char)buffer[i + j * stride];
                if (stride == 2 && buffer[i + j * stride + 1] != 0)
                    match = false;
                else
                    match = char.ToLowerInvariant(c) == char.ToLowerInvariant(text[j]);
            }
            if (!match)
                continue;
            if (requireBoundary && (IsWordChar(buffer, i - stride, stride) || IsWordChar(buffer, i + text.Length * stride, stride)))
                continue;

            for (var j = 0; j < text.Length; j++)
            {
                buffer[i + j * stride] = (byte)'x';
                if (stride == 2)
                    buffer[i + j * stride + 1] = 0;
            }
            i += text.Length * stride - 1;
        }
    }

    private static bool IsWordChar(byte[] buffer, int index, int stride)
    {
        if (index < 0 || index + stride > buffer.Length)
            return false;
        if (stride == 2 && buffer[index + 1] != 0)
            return false;
        var c = (char)buffer[index];
        return char.IsLetterOrDigit(c) || c == '_';
    }
}
