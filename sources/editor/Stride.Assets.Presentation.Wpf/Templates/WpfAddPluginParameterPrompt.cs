// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Assets.Templates;
using Stride.Core.Translation;
using SDDialogResult = Stride.Core.Presentation.Services.DialogResult;

namespace Stride.Assets.Presentation.Templates;

internal sealed class WpfAddPluginParameterPrompt : IAddPluginParameterPrompt
{
    public async Task<AddPluginResult?> PromptAsync(string defaultName, Func<string, bool> isNameTaken, IReadOnlyList<string> referencingProjects)
    {
        // The plugin's namespace is its name, as the template's projects are named after it
        var window = new ProjectLibraryWindow(defaultName, Tr._p("Title", "New plugin"), referencingProjects, showNamespace: false) { LibNameInputValidator = isNameTaken };
        await window.ShowModal();
        return window.Result == SDDialogResult.Ok
            ? new AddPluginResult(window.LibraryName, window.SelectedReferencingProject)
            : null;
    }
}
