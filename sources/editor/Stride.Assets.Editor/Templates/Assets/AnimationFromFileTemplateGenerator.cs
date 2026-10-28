// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// Imports the animations of model files.
/// </summary>
public sealed class AnimationFromFileTemplateGenerator : AssetFromFileTemplateGenerator
{
    public static readonly Guid Id = new("24E65D4C-0207-4B62-B8F2-BAB33F44B4FA");

    /// <inheritdoc/>
    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        return templateDescription.Id == Id;
    }

    /// <inheritdoc/>
    protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
    {
        var files = parameters.Tags.Get(SourceFilesPathKey);
        if (files is null)
            return base.CreateAssets(parameters);

        var importParameters = new AssetImporterParameters { Logger = parameters.Logger };
        importParameters.SelectedOutputTypes.Add(typeof(AnimationAsset), true);
        return ModelFromFileTemplateGenerator.ImportFiles(parameters, files, importParameters);
    }
}
