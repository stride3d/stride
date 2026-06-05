// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Stride.Assets.Templates;
using SDDialogResult = Stride.Core.Presentation.Services.DialogResult;

namespace Stride.Assets.Presentation.Templates;

/// <summary>WPF impl of <see cref="IDotNetNewParameterPrompt"/> — shows <see cref="DotNetNewTemplateParametersWindow"/> modally.</summary>
internal sealed class WpfDotNetNewParameterPrompt : IDotNetNewParameterPrompt
{
    public async Task<IReadOnlyDictionary<string, string>?> PromptAsync(ITemplateInfo template)
    {
        var dialog = new DotNetNewTemplateParametersWindow(template);
        var result = await dialog.ShowModal();
        return result == SDDialogResult.Ok ? dialog.Parameters : null;
    }
}
