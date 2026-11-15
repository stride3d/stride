// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The plugin base classes are toolkit-neutral: a plugin joins a session through <see cref="ISessionViewModel"/> only.
/// </summary>
public class TestAssetsPlugin
{
    // The old plugin API, which the editor still supports, is obsolete
#pragma warning disable CS0672
    public sealed class SessionRecordingPlugin : AssetsEditorPlugin
    {
        public ISessionViewModel? InitializedSession { get; private set; }

        public override void InitializeSession(ISessionViewModel session)
        {
            InitializedSession = session;
        }
    }
#pragma warning restore CS0672

    public sealed class PluginWithArguments : AssetsPlugin
    {
        public PluginWithArguments(int unused)
        {
        }
    }

    [Fact]
    public void PluginsJoinASessionThroughItsInterface()
    {
        var session = new FakeSession();
        var plugin = new SessionRecordingPlugin();
        session.Plugins.Plugins.Add(plugin);

        session.ServiceProvider.Get<IAssetsPluginService>().InitializeSession(session);

        Assert.Same(session, plugin.InitializedSession);
    }

    [Fact]
    public void RegisterPluginRejectsTypesThatCannotBeInstantiated()
    {
        Assert.Throws<ArgumentException>(() => AssetsPlugin.RegisterPlugin(typeof(PluginWithArguments)));
        Assert.Throws<ArgumentException>(() => AssetsPlugin.RegisterPlugin(typeof(string)));
    }

    [Fact]
    public void EditorPluginsRegisterNothingByDefault()
    {
        var plugin = new SessionRecordingPlugin();
        var session = new FakeSession();
        var previewViewModels = new Dictionary<Type, Type>();
        var previewViews = new Dictionary<Type, Type>();
        var enumImages = new Dictionary<object, object>();
        var copyProcessors = new List<ICopyProcessor>();
        var pasteProcessors = new List<IPasteProcessor>();
        var postPasteProcessors = new List<IAssetPostPasteProcessor>();

        // The old plugin API, which the editor still supports, is obsolete
#pragma warning disable STRPLUGIN001, STRPLUGIN002, STRPLUGIN005
        plugin.InitializePlugin(new LoggerResult());
        plugin.RegisterAssetPreviewViewModelTypes(previewViewModels);
        plugin.RegisterAssetPreviewViewTypes(previewViews);
        plugin.RegisterEnumImages(enumImages);
        plugin.RegisterCopyProcessors(copyProcessors, session);
        plugin.RegisterPasteProcessors(pasteProcessors, session);
        plugin.RegisterPostPasteProcessors(postPasteProcessors, session);

        Assert.Empty(previewViewModels);
        Assert.Empty(previewViews);
        Assert.Empty(enumImages);
        Assert.Empty(copyProcessors);
        Assert.Empty(pasteProcessors);
        Assert.Empty(postPasteProcessors);
        Assert.Empty(plugin.ProfileSettings);
#pragma warning restore STRPLUGIN001, STRPLUGIN002, STRPLUGIN005
    }

    [Fact]
    public void ContentReferencesResolveThroughTheSessionInterface()
    {
        var session = new FakeSession();
        var package = session.CreateLocalPackage("Game");
        var asset = package.CreateAsset(package.AssetMountPoint, new AssetItem("MyAsset", new TestSessionAsset()), true, new LoggerResult());

        var reference = ContentReferenceHelper.CreateReference<AssetReference>(asset);

        Assert.NotNull(reference);
        Assert.Equal(asset.Id, reference.Id);
        Assert.Equal(asset.Url, reference.Location);
        Assert.Same(asset, ContentReferenceHelper.GetReferenceTarget(session, reference));
        Assert.Null(ContentReferenceHelper.CreateReference<AssetReference>(null));
    }
}
