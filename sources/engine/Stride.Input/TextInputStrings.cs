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

        return Encoding.UTF8.GetString(bytes);
    }
}
