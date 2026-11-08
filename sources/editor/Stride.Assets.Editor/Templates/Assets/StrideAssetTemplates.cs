// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using Stride.Core.Assets.Templates;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// Registers the asset template generators with the <see cref="TemplateManager"/>, for both editors, each with its own dialogs: the
/// generic factory and import generators first, then the specific ones, which win (the manager searches the most recent generator
/// first).
/// </summary>
public static class StrideAssetTemplates
{
    private static bool registered;

    /// <summary>
    /// Registers the generators once per process.
    /// </summary>
    /// <param name="scriptNamePrompt">The dialog asking the names of a new script.</param>
    /// <param name="modelImportPrompt">The dialog asking the options of a model import.</param>
    public static void Register(IScriptNamePrompt scriptNamePrompt, IModelImportPrompt modelImportPrompt)
    {
        ArgumentNullException.ThrowIfNull(scriptNamePrompt);
        ArgumentNullException.ThrowIfNull(modelImportPrompt);
        if (registered)
            return;
        registered = true;

        TemplateManager.Register(new AssetFactoryTemplateGenerator());
        TemplateManager.Register(new AssetFromFileTemplateGenerator());
        // Specific asset templates must be registered after AssetFactoryTemplateGenerator
        TemplateManager.Register(new ProceduralModelFactoryTemplateGenerator());
        TemplateManager.Register(new SkyboxFactoryTemplateGenerator());
        TemplateManager.Register(new DerivedAssetTemplateGenerator());
        TemplateManager.Register(new ScriptTemplateGenerator(scriptNamePrompt));
        TemplateManager.Register(new SpriteSheetFromFileTemplateGenerator());
        TemplateManager.Register(new ModelFromFileTemplateGenerator(modelImportPrompt));
        TemplateManager.Register(new SkeletonFromFileTemplateGenerator());
        TemplateManager.Register(new AnimationFromFileTemplateGenerator());
    }
}
