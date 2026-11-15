// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Stride.Editor.Avalonia.Engine;

/// <summary>
/// Receives the frames an <see cref="OffscreenFrameExporter"/> reads back from the game (game thread).
/// </summary>
internal interface IOffscreenFrameSink
{
    /// <summary>
    /// Gets a bitmap of the given size to write a frame into, then <see cref="PublishBitmap"/>.
    /// </summary>
    WriteableBitmap AcquireBitmap(int width, int height, PixelFormat format);

    /// <summary>
    /// Publishes a bitmap filled with a frame.
    /// </summary>
    void PublishBitmap(WriteableBitmap bitmap);
}
