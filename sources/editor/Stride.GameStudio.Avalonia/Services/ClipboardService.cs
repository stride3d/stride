// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Input.Platform;
using Stride.Core.Assets.Editor.Services;

namespace Stride.GameStudio.Avalonia.Services;

internal sealed class ClipboardService : IClipboardService
{
    private readonly IClipboard clipboard;

    public ClipboardService(IClipboard clipboard)
    {
        this.clipboard = clipboard;
    }

    public Task ClearAsync()
    {
        return clipboard.ClearAsync();
    }

    public Task<string?> GetTextAsync()
    {
        return clipboard.TryGetTextAsync();
    }

    public Task SetTextAsync(string? text)
    {
        return clipboard.SetTextAsync(text);
    }
}
