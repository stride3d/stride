// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// Registers the Update platforms template of the Update package menu: the <see cref="UpdatePlatformsGenerator"/> with the platforms
/// prompt of an editor, and its synthetic description, which no dotnet new template backs. Both editors register it.
/// </summary>
public static class UpdatePlatformsTemplate
{
    /// <summary>
    /// Registers the generator with the given prompt, and the description.
    /// </summary>
    /// <param name="prompt">The prompt that asks the platforms.</param>
    public static void Register(IUpdatePlatformsParameterPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        TemplateManager.Register(new UpdatePlatformsGenerator(prompt));

        var description = new TemplateDescription
        {
            Id = UpdatePlatformsGenerator.TemplateId,
            Name = Tr._p("Template", "Update platforms"),
            Description = Tr._p("Template", "Add or update per-platform executable projects for the current game"),
            Group = "General",
            Scope = TemplateScope.Package,
            DefaultOutputName = "MyGame",
            FullPath = new UFile("Stride.UpdatePlatforms.synthetic.sdtpl"),
        };
        // In a synthetic package, since TemplateManager.FindTemplates goes through the templates of the packages
        var package = new Package { FullPath = new UFile("Stride.UpdatePlatforms.synthetic") };
        package.Templates.Add(description);
        TemplateManager.RegisterPackage(package);
    }
}
