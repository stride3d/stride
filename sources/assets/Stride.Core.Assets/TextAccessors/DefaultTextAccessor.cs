// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

namespace Stride.Core.Assets.TextAccessors;

public class DefaultTextAccessor : ITextAccessor
{
    private static readonly Encoding WithoutByteOrderMark = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    // UTF-32 little endian first: its mark starts with the one of UTF-16 little endian.
    private static readonly Encoding[] EncodingsWithByteOrderMark =
    [
        Encoding.UTF32,
        Encoding.UTF8,
        Encoding.Unicode,
        Encoding.BigEndianUnicode,
        new UTF32Encoding(bigEndian: true, byteOrderMark: true),
    ];

    private string? text;
    // The encoding of the file once read: with its byte order mark when the file had one.
    private Encoding? encoding;

    public string? FilePath { get; internal set; }

    /// <inheritdoc/>
    public string Get()
    {
        return text ??= (FilePath != null ? LoadFromFile() : FilePath) ?? "";
    }

    /// <inheritdoc/>
    public void Set(string? value)
    {
        text = value;
    }

    /// <summary>
    /// Writes the text in the encoding of the file, with a byte order mark only when the file had one; a new file gets UTF-8 without a mark.
    /// </summary>
    public async Task Save(Stream stream)
    {
        if (text != null)
        {
            using var streamWriter = new StreamWriter(stream, encoding ?? ReadFileEncoding(), 1024, true);
            await streamWriter.WriteAsync(text);
        }
        else if (FilePath != null)
        {
            using var inputStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096, useAsync: true);
            await inputStream.CopyToAsync(stream);
        }
    }

    public ISerializableTextAccessor GetSerializableVersion()
    {
        // Still not loaded?
        if (text == null && FilePath != null)
            return new FileTextAccessor { FilePath = FilePath };

        return new StringTextAccessor { Text = text };
    }

    /// <summary>
    /// Sets the text from the content of the file, and keeps its encoding for <see cref="Save"/>.
    /// </summary>
    internal void Load(ReadOnlySpan<byte> content)
    {
        encoding = DetectEncoding(content, out var byteOrderMarkLength);
        text = encoding.GetString(content[byteOrderMarkLength..]);
    }

    private static Encoding DetectEncoding(ReadOnlySpan<byte> content, out int byteOrderMarkLength)
    {
        foreach (var candidate in EncodingsWithByteOrderMark)
        {
            var byteOrderMark = candidate.Preamble;
            if (content.StartsWith(byteOrderMark))
            {
                byteOrderMarkLength = byteOrderMark.Length;
                return candidate;
            }
        }
        byteOrderMarkLength = 0;
        return WithoutByteOrderMark;
    }

    // The text was set without reading the file: the file is still the one on disk (assets are saved to memory first).
    private Encoding ReadFileEncoding()
    {
        if (FilePath == null)
            return WithoutByteOrderMark;

        try
        {
            using var file = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            Span<byte> start = stackalloc byte[4];
            var read = file.ReadAtLeast(start, start.Length, throwOnEndOfStream: false);
            return DetectEncoding(start[..read], out _);
        }
        catch (IOException)
        {
            return WithoutByteOrderMark;
        }
        catch (UnauthorizedAccessException)
        {
            return WithoutByteOrderMark;
        }
    }

    private string? LoadFromFile()
    {
        if (FilePath == null)
            return null;

        try
        {
            Load(File.ReadAllBytes(FilePath));
            return text;
        }
        catch
        {
            return null;
        }
    }
}
