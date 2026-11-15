// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Text;

namespace Stride.Input;

/// <summary>
///   Decodes the text carried by platform text input events into the strings exposed by <see cref="TextInputEvent.Text"/>.
/// </summary>
internal static class TextInputStrings
{
    private const int AsciiCount = 128;

    // Typing mostly produces one ASCII character per event, so those strings are shared instead of allocated per event
    private static readonly string[] SingleAsciiCharacters = CreateSingleAsciiCharacters();

    /// <summary>
    ///   Decodes null-terminated UTF-8 text from a fixed-size buffer.
    /// </summary>
    /// <param name="text">The start of the buffer.</param>
    /// <param name="capacity">The size of the buffer in bytes. Decoding stops here if no terminator is found first.</param>
    public static unsafe string FromNullTerminatedUtf8(byte* text, int capacity)
    {
        var bytes = new ReadOnlySpan<byte>(text, capacity);
        var length = bytes.IndexOf((byte)0);
        if (length >= 0)
            bytes = bytes[..length];

        if (bytes.Length == 1 && bytes[0] < AsciiCount)
            return SingleAsciiCharacters[bytes[0]];

        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    ///   Creates a string from UTF-16 text.
    /// </summary>
    /// <param name="text">The text, without a terminator.</param>
    public static string FromUtf16(ReadOnlySpan<char> text)
    {
        if (text.Length == 1 && text[0] < AsciiCount)
            return SingleAsciiCharacters[text[0]];

        return new string(text);
    }

    private static string[] CreateSingleAsciiCharacters()
    {
        var strings = new string[AsciiCount];
        for (int i = 0; i < AsciiCount; i++)
            strings[i] = ((char)i).ToString();
        return strings;
    }
}
