// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;
using Stride.Rendering;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// The options of a model import (see <see cref="ModelFromFileTemplateGenerator"/>): what to import with the model, and the
/// skeleton to use instead of the model's.
/// </summary>
public sealed class ImportModelFromFileViewModel : DispatcherViewModel
{
    private readonly SessionViewModel session;
    private bool importMaterials = true;
    private bool showDeduplicateMaterialsCheckBox = true;
    private bool showFbxDedupeNotSupportedWarning;
    private bool deduplicateMaterials = true;
    private bool importTextures = true;
    private bool importAnimations = true;
    private bool importSkeleton = true;
    private bool dontImportSkeleton;
    private bool reuseSkeleton;
    private AssetViewModel? skeletonToReuse;

    /// <param name="session">The session the model is imported in, whose skeletons can be reused.</param>
    public ImportModelFromFileViewModel(SessionViewModel session)
        : base(session.SafeArgument(nameof(session)).ServiceProvider)
    {
        this.session = session;
        BrowseSkeletonCommand = new AnonymousTaskCommand(ServiceProvider, BrowseSkeletonAsync);
        ClearSkeletonCommand = new AnonymousCommand(ServiceProvider, () => SkeletonToReuse = null);
        DependentProperties.Add(nameof(SkeletonToReuse), [nameof(SkeletonName)]);
    }

    public string Title => Tr._p("Title", "Import from model");

    public bool ImportMaterials { get => importMaterials; set => SetValue(ref importMaterials, value); }

    public bool ShowDeduplicateMaterialsCheckBox { get => showDeduplicateMaterialsCheckBox; set => SetValue(ref showDeduplicateMaterialsCheckBox, value); }

    public bool ShowFbxDedupeNotSupportedWarning { get => showFbxDedupeNotSupportedWarning; set => SetValue(ref showFbxDedupeNotSupportedWarning, value); }

    public bool DeduplicateMaterials { get => deduplicateMaterials; set => SetValue(ref deduplicateMaterials, value); }

    public bool ImportTextures { get => importTextures; set => SetValue(ref importTextures, value); }

    public bool ImportAnimations { get => importAnimations; set => SetValue(ref importAnimations, value); }

    /// <summary>
    /// Whether the skeleton of the model is imported; one of <see cref="ImportSkeleton"/>, <see cref="DontImportSkeleton"/> and
    /// <see cref="ReuseSkeleton"/> holds.
    /// </summary>
    public bool ImportSkeleton
    {
        get => importSkeleton;
        set
        {
            if (SetValue(ref importSkeleton, value) && value)
            {
                DontImportSkeleton = false;
                ReuseSkeleton = false;
            }
        }
    }

    /// <summary>
    /// Whether the model gets no skeleton.
    /// </summary>
    public bool DontImportSkeleton
    {
        get => dontImportSkeleton;
        set
        {
            if (SetValue(ref dontImportSkeleton, value) && value)
            {
                ImportSkeleton = false;
                ReuseSkeleton = false;
            }
        }
    }

    /// <summary>
    /// Whether the model uses <see cref="SkeletonToReuse"/>.
    /// </summary>
    public bool ReuseSkeleton
    {
        get => reuseSkeleton;
        set
        {
            if (SetValue(ref reuseSkeleton, value) && value)
            {
                ImportSkeleton = false;
                DontImportSkeleton = false;
            }
        }
    }

    /// <summary>
    /// The skeleton asset the model uses when <see cref="ReuseSkeleton"/>, or <c>null</c>.
    /// </summary>
    public AssetViewModel? SkeletonToReuse
    {
        get => skeletonToReuse;
        set => SetValue(ref skeletonToReuse, value);
    }

    /// <summary>
    /// The name of <see cref="SkeletonToReuse"/>, for the dialog.
    /// </summary>
    public string SkeletonName => skeletonToReuse?.Url ?? Tr._p("Label", "(none)");

    /// <summary>
    /// Picks <see cref="SkeletonToReuse"/> among the skeletons of the session, and selects <see cref="ReuseSkeleton"/>.
    /// </summary>
    public ICommandBase BrowseSkeletonCommand { get; }

    public ICommandBase ClearSkeletonCommand { get; }

    private async Task BrowseSkeletonAsync()
    {
        var dialogs = ServiceProvider.Get<IEditorDialogService>();
        var skeleton = await dialogs.OpenAssetPickerAsync(session, AssetRegistry.GetAssetTypes(typeof(Skeleton)), initialAsset: SkeletonToReuse);
        if (skeleton is not null)
        {
            SkeletonToReuse = skeleton;
            ReuseSkeleton = true;
        }
    }
}
