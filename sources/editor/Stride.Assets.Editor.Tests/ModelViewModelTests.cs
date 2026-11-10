// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Templates;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Serialization;
using Stride.Importer.Common;
using Stride.Rendering;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The commands of the model asset (<see cref="ModelViewModel"/>): Create skeleton runs the skeleton import template of the
/// session on the model's source file, as the WPF editor does.
/// </summary>
public sealed class ModelViewModelTests
{
    private static readonly Lazy<bool> ImporterRegistered = new(() =>
    {
        // Process-wide, as the editor's importers are; only files of the test extension reach it
        AssetRegistry.RegisterImporter(new TestModelImporter());
        return true;
    });

    [Fact]
    public async Task TheModelHasTheCreateSkeletonCommand()
    {
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Robot", new ModelAsset { Source = new UFile("Robot.fbx") }));

        var model = fixture.Asset<ModelViewModel>("Robot");

        // The icon is a resource key that the view resolves
        var command = Assert.Single(model.AssetCommands, x => x.DisplayName == "Create Skeleton");
        Assert.Same(model.CreateSkeletonCommand, command.Command);
        Assert.Equal("ImageCreateSkeleton", command.Icon);
        Assert.Equal("Create a skeleton asset", command.Tooltip);
    }

    [Fact]
    public async Task CreateSkeletonImportsTheSkeletonOfTheSourceAndReferencesItInOneUndoableAction()
    {
        _ = ImporterRegistered.Value;
        var generator = new SkeletonFromFileTemplateGenerator();
        TemplateManager.Register(generator);
        try
        {
            using var fixture = await OpenAsync(new ModelAsset());
            var model = fixture.Asset<ModelViewModel>("Robot");
            // The source is in the resource folder of the package, so the import does not offer to copy it there
            var resources = fixture.Package.Package.ResourceFolders.First();
            Directory.CreateDirectory(resources);
            var source = new UFile(Path.Combine(resources, "Robot" + TestModelImporter.Extension));
            File.WriteAllText(source, "");
            model.Asset.Source = source;
            var undoRedo = fixture.ServiceProvider.Get<IUndoRedoService>();
            var transactions = fixture.Session.ActionHistory.Transactions.Count;

            await fixture.Session.Dispatcher.InvokeTask(model.CreateSkeletonAsync);
            WaitForHistory(fixture, transactions);

            var skeleton = Assert.Single(fixture.Session.AllAssets, x => x.Asset is SkeletonAsset);
            Assert.Equal(["Root", "Hip"], ((SkeletonAsset)skeleton.Asset).Nodes.Select(x => x.Name));
            Assert.Equal(skeleton.Id, AttachedReferenceManager.GetAttachedReference(model.Asset.Skeleton)?.Id);
            // The import and the reference are one entry of the history
            Assert.Equal(transactions + 1, fixture.Session.ActionHistory.Transactions.Count);
            Assert.Equal("Create Skeleton", fixture.Session.ActionHistory.Transactions.Last().Name);

            fixture.Session.Dispatcher.Invoke(undoRedo.Undo);

            Assert.Null(model.Asset.Skeleton);
            Assert.DoesNotContain(fixture.Session.AllAssets, x => x.Asset is SkeletonAsset);
        }
        finally
        {
            TemplateManager.Unregister(generator);
        }
    }

    [Fact]
    public async Task CreateSkeletonDoesNothingWithoutASourceFile()
    {
        using var fixture = await OpenAsync(new ModelAsset());
        var model = fixture.Asset<ModelViewModel>("Robot");
        var transactions = fixture.Session.ActionHistory.Transactions.Count;

        await fixture.Session.Dispatcher.InvokeTask(model.CreateSkeletonAsync);

        Assert.Null(model.Asset.Skeleton);
        Assert.DoesNotContain(fixture.Session.AllAssets, x => x.Asset is SkeletonAsset);
        Assert.Equal(transactions, fixture.Session.ActionHistory.Transactions.Count);
    }

    /// <summary>
    /// Opens a session with the model, and the skeleton import template of the Stride package (<c>DefaultSkeleton.sdtpl</c>)
    /// in the session's package, so that it goes away with the session.
    /// </summary>
    private static async Task<AssetsSessionFixture> OpenAsync(ModelAsset model)
    {
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Robot", model));
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        fixture.Package.Package.Templates.Add(new TemplateAssetDescription
        {
            Id = ModelViewModel.SkeletonFromFileTemplateId,
            Name = "Skeleton",
            Scope = TemplateScope.Asset,
            AssetTypeName = nameof(SkeletonAsset),
            DefaultOutputName = "Skeleton",
            Group = "Animation",
            ImportSource = true,
            FullPath = new UFile(Path.Combine(fixture.Directory, "DefaultSkeleton" + TemplateDescription.FileExtension)),
        });
        // The asset view creates the new assets in its selected folder
        fixture.Session.Dispatcher.Invoke(() => fixture.Session.AssetCollection.SelectedLocations.Add(fixture.Package));
        return fixture;
    }

    /// <summary>
    /// The action history records a creation once the asset notifications are done, on another thread: wait for it, so that the
    /// test neither reads the history nor closes the session in the middle of them.
    /// </summary>
    private static void WaitForHistory(AssetsSessionFixture fixture, int transactions)
    {
        Assert.True(SpinWait.SpinUntil(() => fixture.Session.ActionHistory.Transactions.Count > transactions, TimeSpan.FromSeconds(10)), "the creation to be recorded");
    }

    /// <summary>
    /// A model importer of a test extension, whose files hold a two-node skeleton, so that the model import code runs without
    /// the native importers.
    /// </summary>
    private sealed class TestModelImporter : ModelAssetImporter
    {
        public const string Extension = ".sdtestmodel";

        public override Guid Id { get; } = new("8C1A0B7E-4D2F-4E63-9A51-6B7C8D9E0F12");

        public override string Description => "Test models";

        public override string SupportedFileExtensions => Extension;

        public override EntityInfo GetEntityInfo(UFile localPath, Logger logger, AssetImporterParameters importParameters)
        {
            return new EntityInfo
            {
                Nodes =
                [
                    new NodeInfo { Name = "Root", Depth = 0, Preserve = true },
                    new NodeInfo { Name = "Hip", Depth = 1, Preserve = true },
                ],
            };
        }

        public override void GetAnimationDuration(UFile localPath, Logger logger, AssetImporterParameters importParameters, int animIndex, out TimeSpan startTime, out TimeSpan endTime)
        {
            startTime = TimeSpan.Zero;
            endTime = TimeSpan.Zero;
        }
    }
}
