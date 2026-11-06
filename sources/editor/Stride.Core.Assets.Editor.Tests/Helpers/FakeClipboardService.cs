// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// An in-memory clipboard.
/// </summary>
public sealed class FakeClipboardService : IClipboardService
{
    public string? Text { get; set; }

    public Task<string?> GetTextAsync() => Task.FromResult(Text);

    public Task SetTextAsync(string? text)
    {
        Text = text;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Text = null;
        return Task.CompletedTask;
    }
}
