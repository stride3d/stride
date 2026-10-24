// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Logs;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Common.Tests;

/// <summary>
/// The session objects work on a session without any UI, through <see cref="ISessionViewModel"/>.
/// </summary>
public class TestSessionObjects
{
    private readonly FakeSession session = new();

    [Fact]
    public void PackagesAddThemselvesToTheSession()
    {
        var local = session.CreateLocalPackage("Game");
        var store = session.CreateStorePackage("Library");

        Assert.Equal([local], session.LocalPackages);
        Assert.Equal([store], session.StorePackages);
        Assert.True(local.IsEditable);
        Assert.False(store.IsEditable);
    }

    [Fact]
    public void PackageDirtiablesIncludeTheSessionOnes()
    {
        var other = session.CreateStorePackage("Library");
        session.Dirtiables = [other];
        var package = session.CreateLocalPackage("Game");

        Assert.Contains(package, package.Dirtiables);
        Assert.Contains(other, package.Dirtiables);
    }

    [Fact]
    public void AssetDirectoriesAreCreatedOnDemand()
    {
        var package = session.CreateLocalPackage("Game");

        var directory = package.GetOrCreateAssetDirectory("Materials/Metal", false);

        Assert.Equal("Metal", directory.Name);
        Assert.Equal("Materials/Metal/", directory.Path);
        Assert.Equal("Materials", directory.Parent.Name);
        Assert.Same(package.AssetMountPoint, directory.Parent.Parent);
        Assert.Same(directory, package.GetOrCreateAssetDirectory("Materials/Metal", false));
    }

    [Fact]
    public void DirectoryRenameIsUndoable()
    {
        var package = session.CreateLocalPackage("Game");
        var directory = package.GetOrCreateAssetDirectory("Textures", false);

        directory.Name = "Images";
        Assert.Equal("Images/", directory.Path);

        session.UndoRedo.Undo();
        Assert.Equal("Textures", directory.Name);
        Assert.Empty(session.Messages);
    }

    [Fact]
    public void DirectoryRenameToAnExistingNameShowsAMessage()
    {
        var package = session.CreateLocalPackage("Game");
        package.GetOrCreateAssetDirectory("Textures", false);
        var directory = package.GetOrCreateAssetDirectory("Images", false);

        directory.Name = "textures";

        Assert.Equal("Images", directory.Name);
        var message = Assert.Single(session.Messages);
        Assert.Equal("Unable to rename folder. A folder with the same name already exists in the parent folder.", message);
    }

    [Fact]
    public void PackageRenameIsUndoable()
    {
        var package = session.CreateLocalPackage("Game");

        package.Name = "Renamed";
        Assert.Equal("/fake/Game/Renamed.sdpkg", package.PackagePath.ToString());

        session.UndoRedo.Undo();
        Assert.Equal("Game", package.Name);
    }

    [Fact]
    public void PackageRenameToAnExistingNameShowsAMessage()
    {
        session.CreateLocalPackage("Game");
        var package = session.CreateLocalPackage("Other");

        package.Name = "game";

        Assert.Equal("Other", package.Name);
        var message = Assert.Single(session.Messages);
        Assert.Equal("This package couldn't be renamed. A package with the same name already exists in the session.", message);
    }

    [Fact]
    public void MountPointsCannotBeDroppedInADirectory()
    {
        var package = session.CreateLocalPackage("Game");
        var directory = (IAddChildViewModel)package.GetOrCreateAssetDirectory("Textures", false);

        // The mount points of the editor are read-only, which is checked first
        Assert.False(directory.CanAddChildren([new EditableMountPoint(package)], AddChildModifiers.None, out var message));
        Assert.Equal(DragDropBehavior.InvalidDropAreaMessage, message);
        Assert.Equal("Can't drop here", message);
    }

    [Fact]
    public void CreatedAssetsRegisterInTheSessionAndPackage()
    {
        var package = session.CreateLocalPackage("Game");
        var directory = package.GetOrCreateAssetDirectory("Data", false);
        var assetItem = new AssetItem("Data/MyAsset", new TestSessionAsset { Value = "Value" });

        var asset = package.CreateAsset(directory, assetItem, true, new LoggerResult());

        Assert.IsType<AssetViewModel<TestSessionAsset>>(asset);
        Assert.Same(asset, session.GetAssetById(asset.Id));
        Assert.Contains(assetItem, package.Package.Assets);
        Assert.Equal([asset], directory.Assets);
        Assert.Equal("Data/MyAsset", asset.Url);
        Assert.NotNull(asset.PropertyGraph);
        Assert.Same(asset.PropertyGraph, session.GraphContainer.TryGetGraph(asset.Id));
    }

    [Fact]
    public void AssetCommandsUseIconKeys()
    {
        var package = session.CreateLocalPackage("Game");
        var asset = package.CreateAsset(package.AssetMountPoint, new AssetItem("MyAsset", new TestSessionAsset()), true, new LoggerResult());

        // The view resolves the keys to images (see MenuCommandInfo.Icon)
        Assert.Equal(
            [("Create derived asset", "ImageCopyLink"), ("Create replacing asset", "ImageCopyLink"), ("Clear archetype", "ImageDeleteLink"), ("Clear replaces", "ImageDeleteLink")],
            asset.AssetCommands.Select(x => (x.DisplayName, (string)x.Icon)));
    }

    [Fact]
    public void AssetDeletionIsUndoable()
    {
        var package = session.CreateLocalPackage("Game");
        var assetItem = new AssetItem("MyAsset", new TestSessionAsset());
        var asset = package.CreateAsset(package.AssetMountPoint, assetItem, true, new LoggerResult());

        asset.IsDeleted = true;

        Assert.Null(session.GetAssetById(asset.Id));
        Assert.DoesNotContain(assetItem, package.Package.Assets);
        Assert.Equal([asset], package.DeletedAssets);
        Assert.Null(session.GraphContainer.TryGetGraph(asset.Id));

        session.UndoRedo.Undo();

        Assert.Same(asset, session.GetAssetById(asset.Id));
        Assert.Contains(assetItem, package.Package.Assets);
        Assert.Empty(package.DeletedAssets);
        Assert.Same(asset.PropertyGraph, session.GraphContainer.TryGetGraph(asset.Id));
    }

    [Fact]
    public void ConsistencyCheckLogsAssetsWithoutViewModel()
    {
        var package = session.CreateLocalPackage("Game");
        package.CreateAsset(package.AssetMountPoint, new AssetItem("MyAsset", new TestSessionAsset()), true, new LoggerResult());
        var logKey = LogKey.Get("Consistency Game");

        package.CheckConsistency();
        Assert.Equal([logKey], session.Log.Cleared);
        Assert.Empty(session.Log[logKey].Messages);

        package.Package.Assets.Add(new AssetItem("Orphan", new TestSessionAsset()));
        package.CheckConsistency();
        var message = Assert.Single(session.Log[logKey].Messages);
        Assert.Equal(LogMessageType.Fatal, message.Type);
        Assert.Contains("Orphan", message.Text);
    }

    [Fact]
    public void FolderThumbnailComesFromTheFactory()
    {
        var package = session.CreateLocalPackage("Game");
        var directory = (DirectoryViewModel)package.GetOrCreateAssetDirectory("Textures", false);

        // The folder thumbnail is shared by all folders: it is created once, by the first session that asks for it
        var thumbnail = Assert.IsType<FakeThumbnailData>(directory.ThumbnailData);
        Assert.Same(thumbnail, package.GetOrCreateAssetDirectory("Images", false) is DirectoryViewModel other ? other.ThumbnailData : null);
        Assert.Equal(["FolderIconAlfredo"], session.Thumbnails.ResourceKeys);
    }

    [Fact]
    public void LogKeysCompareByAssetAndName()
    {
        var assetId = AssetId.New();

        Assert.Equal(LogKey.Get("Name"), LogKey.Get("Name"));
        Assert.True(LogKey.Get(assetId, "Name") == LogKey.Get(assetId, "Name"));
        Assert.True(LogKey.Get(assetId, "Name") != LogKey.Get("Name"));
        Assert.NotEqual(LogKey.Get("Name"), LogKey.Get("Other"));
        Assert.Throws<ArgumentException>(() => LogKey.Get(AssetId.Empty, "Name"));
    }

    private sealed class EditableMountPoint(PackageViewModel package) : MountPointViewModel(package)
    {
        public override string Name { get => "Editable"; set => throw new NotSupportedException(); }

        public override bool IsEditable => true;

        public override bool CanDelete(out string error)
        {
            error = "";
            return false;
        }

        public override bool AcceptAssetType(Type assetType) => true;
    }
}
