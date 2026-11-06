// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Common.Tests;

/// <summary>
/// The plugin base classes are toolkit-neutral: a plugin joins a session through <see cref="ISessionViewModel"/> only.
/// </summary>
public class TestAssetsPlugin
{
    public sealed class SessionRecordingPlugin : AssetsEditorPlugin
    {
        public ISessionViewModel? InitializedSession { get; private set; }

        public ISessionViewModel? DisposedSession { get; private set; }

        public override void InitializeSession(ISessionViewModel session)
        {
            InitializedSession = session;
        }

        protected internal override void SessionDisposed(ISessionViewModel session)
        {
            DisposedSession = session;
        }
    }

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
        plugin.SessionDisposed(session);

        Assert.Same(session, plugin.InitializedSession);
        Assert.Same(session, plugin.DisposedSession);
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
