// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Templates;
using Stride.Core.Assets.Quantum.Templates;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// Registers the project template generators of the Avalonia editor with their prompts, as <c>StrideTemplates</c> does for WPF.
/// The asset template generators are not registered yet (the asset templates come with a later change).
/// </summary>
internal static class StrideAvaloniaTemplates
{
    private static bool registered;

    public static void Register()
    {
        if (registered)
            return;
        registered = true;

        // The session generators are wrapped by the Quantum-aware decorator, so that the override metadata is materialized through the
        // asset graph before the save
        QuantumTemplateRegistration.Register(new DotNetNewTemplateGenerator(new AvaloniaDotNetNewParameterPrompt()));
        // After DotNetNew so that they take precedence for the library and plugin templates (the most recent generator wins)
        QuantumTemplateRegistration.Register(new AddLibraryGenerator(new AvaloniaAddLibraryParameterPrompt()));
        QuantumTemplateRegistration.Register(new AddPluginGenerator(new AvaloniaAddPluginParameterPrompt()));
    }
}
