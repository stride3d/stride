// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Assets.Templates;
using SDDialogResult = Stride.Core.Presentation.Services.DialogResult;

namespace Stride.Assets.Presentation.Templates;

/// <summary>WPF impl of <see cref="IAddLibraryParameterPrompt"/> — shows <see cref="ProjectLibraryWindow"/> modally.</summary>
internal sealed class WpfAddLibraryParameterPrompt : IAddLibraryParameterPrompt
{
    public async Task<AddLibraryResult?> PromptAsync(string defaultName, Func<string, bool> isNameTaken)
    {
        var window = new ProjectLibraryWindow(defaultName) { LibNameInputValidator = isNameTaken };
        await window.ShowModal();
        return window.Result == SDDialogResult.Ok
            ? new AddLibraryResult(window.LibraryName, window.Namespace)
            : null;
    }
}
