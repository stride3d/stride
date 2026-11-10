// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Assets.Editor.Templates;
using SDDialogResult = Stride.Core.Presentation.Services.DialogResult;

namespace Stride.Assets.Presentation.Templates;

/// <summary>The WPF <see cref="IScriptNamePrompt"/>: shows <see cref="ScriptNameWindow"/> modally.</summary>
internal sealed class WpfScriptNamePrompt : IScriptNamePrompt
{
    public async Task<bool> PromptAsync(ScriptNameViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        var window = new ScriptNameWindow(viewModel);
        await window.ShowModal();
        return window.Result == SDDialogResult.Ok;
    }
}
