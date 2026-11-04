// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Assets.Editor.Templates;
using SDDialogResult = Stride.Core.Presentation.Services.DialogResult;

namespace Stride.Assets.Presentation.Templates;

/// <summary>The WPF <see cref="IModelImportPrompt"/>: shows <see cref="ModelAssetTemplateWindow"/> modally.</summary>
internal sealed class WpfModelImportPrompt : IModelImportPrompt
{
    public async Task<bool> PromptAsync(ImportModelFromFileViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        var window = new ModelAssetTemplateWindow(viewModel);
        try
        {
            await window.ShowModal();
            if (window.Result != SDDialogResult.Ok)
                return false;

            window.ApplySkeleton();
            return true;
        }
        finally
        {
            window.ReferenceViewModel.Destroy();
        }
    }
}
