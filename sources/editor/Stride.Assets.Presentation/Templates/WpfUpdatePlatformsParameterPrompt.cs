// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Templates;
using Stride.Core;
using SDDialogResult = Stride.Core.Presentation.Services.DialogResult;

namespace Stride.Assets.Presentation.Templates;

/// <summary>WPF impl of <see cref="IUpdatePlatformsParameterPrompt"/> — shows <see cref="UpdatePlatformsWindow"/> modally.</summary>
internal sealed class WpfUpdatePlatformsParameterPrompt : IUpdatePlatformsParameterPrompt
{
    public async Task<UpdatePlatformsResult?> PromptAsync(ISet<PlatformType> installed)
    {
        var window = new UpdatePlatformsWindow(installed);
        await window.ShowModal();
        if (window.Result != SDDialogResult.Ok)
            return null;

        var selected = window.SelectedPlatforms.Select(p => p.Platform.Type).ToList();
        return new UpdatePlatformsResult(selected, window.ForcePlatformRegeneration);
    }
}
