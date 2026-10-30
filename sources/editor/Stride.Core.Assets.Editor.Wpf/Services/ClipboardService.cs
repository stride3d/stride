// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Interop;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// The clipboard of the WPF editor, for the shared commands that copy and paste text (property copy and paste).
    /// </summary>
    /// <remarks>
    /// The WPF clipboard is synchronous: the tasks are already completed, and the errors of <see cref="SafeClipboard"/> are kept.
    /// </remarks>
    internal sealed class ClipboardService : IClipboardService
    {
        public Task<string?> GetTextAsync()
        {
            return Task.FromResult<string?>(SafeClipboard.GetText());
        }

        public Task SetTextAsync(string? text)
        {
            SafeClipboard.SetText(text ?? string.Empty);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            try
            {
                Clipboard.Clear();
            }
            catch (COMException e) when (e.HResult == SafeClipboard.CLIPBRD_E_CANT_OPEN)
            {
                e.Ignore();
            }
            return Task.CompletedTask;
        }
    }
}
