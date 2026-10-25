// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="GameSettingsAsset"/>.
/// </summary>
[AssetViewModel<GameSettingsAsset>]
public sealed class GameSettingsViewModel : AssetViewModel<GameSettingsAsset>
{
    public const string AvailableFilters = "AvailableFilters";

    private RequiredDisplayOrientation displayOrientation;

    public GameSettingsViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
        displayOrientation = Asset.GetOrCreate<RenderingSettings>().DisplayOrientation;
    }

    /// <inheritdoc />
    public override bool IsLocked => false;

    public SceneViewModel? DefaultScene
    {
        get
        {
            if (Asset.DefaultScene == null)
                return null;

            var reference = AttachedReferenceManager.GetAttachedReference(Asset.DefaultScene);
            return Session.GetAssetById(reference!.Id) as SceneViewModel;
        }
        set => SetValue(DefaultScene != value, () => UpdateGameSettings(value));
    }

    /// <inheritdoc />
    protected override void OnSessionSaved()
    {
        base.OnSessionSaved();

        // Display orientation needs changes in iOS / Android manifest files
        var currentOrientation = Asset.GetOrCreate<RenderingSettings>().DisplayOrientation;
        if (displayOrientation != currentOrientation && Session.CurrentProject != null)
        {
            GameSettingsAssetCompiler.SetPlatformOrientation(Session.CurrentProject.Project, currentOrientation);

            displayOrientation = currentOrientation;
        }
    }

    private void UpdateGameSettings(SceneViewModel? scene)
    {
        Asset.DefaultScene = scene != null ? ContentReferenceHelper.CreateReference<Scene>(scene) : null;
    }
}
