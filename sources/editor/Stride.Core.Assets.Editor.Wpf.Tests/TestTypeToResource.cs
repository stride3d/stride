// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View.ValueConverters;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Xunit;

#nullable enable

namespace Stride.Core.Assets.Editor.Wpf.Tests
{
    /// <summary>
    /// The WPF type images come from the plugin host, as in the cross-platform editor: the images that the plugins of the
    /// session register and the ones that the registered assemblies declare.
    /// </summary>
    public sealed class TestTypeToResource
    {
        [Fact]
        public void TypeToResourceAsksThePluginHostAlongTheBaseTypes()
        {
            var pluginService = new ImagesPluginService(new() { [typeof(HostBase)] = "host-image" });

            Assert.Equal("host-image", TypeToResource.FetchResourceFromType(typeof(HostBase), true, pluginService));
            Assert.Equal("host-image", TypeToResource.FetchResourceFromType(typeof(HostDerived), true, pluginService));
            Assert.Null(TypeToResource.FetchResourceFromType(typeof(HostDerived), false, pluginService));
            Assert.Null(TypeToResource.FetchResourceFromType(typeof(HostBase), true, null!));
        }

        private class HostBase;

        private sealed class HostDerived : HostBase;

        /// <summary>
        /// A plugin service with type images only.
        /// </summary>
        private sealed class ImagesPluginService(Dictionary<Type, object> images) : IAssetsPluginService
        {
            public IReadOnlyList<AssetsPlugin> Plugins => [];

            public object? GetImageForType(Type type) => images.GetValueOrDefault(type);

            public bool HasImagesForEnum(ISessionViewModel? session, Type enumType) => false;

            public object? GetImageForEnum(ISessionViewModel? session, object value) => null;

            public bool HasEditorView(ISessionViewModel session, Type viewModelType) => false;

            public Type? GetAssetViewModelType(Type assetType) => null;

            public Type? GetEditorViewModelType(Type viewModelType) => null;

            public Type? GetEditorViewType(Type editorViewModelType) => null;

            public Type? GetPreviewViewModelType(Type previewType) => null;

            public Type? GetPreviewViewType(Type previewType) => null;

            public void RegisterSession(ISessionViewModel session, ILogger logger) => throw new NotSupportedException();

            public void InitializeSession(ISessionViewModel session) => throw new NotSupportedException();

            public IEnumerable<Type> GetPrimitiveTypes(ISessionViewModel session) => [];

            public IEnumerable<PackageSettingsEntry> GetProfileSettings() => [];
        }

        /// <summary>
        /// A plugin exposing the type images to the tests.
        /// </summary>
        private sealed class TestImagesPlugin : AssetsEditorPlugin
        {
            public static void Register(Type type, object image) => TypeImages[type] = image;
        }
    }
}
