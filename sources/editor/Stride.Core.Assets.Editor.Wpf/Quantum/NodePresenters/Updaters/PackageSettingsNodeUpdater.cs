// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    // Synthesizes the "Build" rows of the package property grid as virtual presenters backed by csproj
    // msbuild properties: an explicitly set property renders as an override (bold, resettable) while an
    // unset one shows the computed default.
    internal sealed class PackageSettingsNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (node.Parent != null || node.Value is not PackageSettingsWrapper wrapper || wrapper.ProjectPath == null)
                return;

            var category = node.GetCategory("Build") ?? node.CreateCategory("Build", null, null);

            if (wrapper.IsWindowsExecutable)
            {
                AddProjectPropertyNode(node, category, wrapper, PackageSettingsWrapper.GraphicsApiProperty,
                    "GraphicsApi", "Graphics API", typeof(GameGraphicsApi), 10,
                    "The graphics API the game is built and run with. Default uses the platform default.",
                    raw => Enum.TryParse<GameGraphicsApi>(raw, ignoreCase: true, out var api) && Enum.IsDefined(api) ? api : GameGraphicsApi.Default,
                    value => (GameGraphicsApi)value == GameGraphicsApi.Default ? null : value.ToString());
            }

            AddProjectPropertyNode(node, category, wrapper, PackageSettingsWrapper.ContainsAssetTypesProperty,
                "ContainsAssetTypes", "Contains asset types", typeof(bool), 20,
                "Whether this project's assembly defines types used in assets (scripts, components, custom asset types), " +
                "so the editor and asset compiler load it and packing declares it to consumers. " +
                "Unset, libraries are loaded and other projects are not.",
                raw => bool.TryParse(raw, out var value) ? value : wrapper.ContainsAssetTypesDefault,
                value => (bool)value ? "true" : "false");
        }

        private static void AddProjectPropertyNode(IAssetNodePresenter node, INodePresenter category, PackageSettingsWrapper wrapper,
            string propertyName, string name, string displayName, Type type, int order, string documentation,
            Func<string, object> fromProperty, Func<object, string> toProperty)
        {
            bool IsExplicit() => wrapper.GetProjectProperty(propertyName) != null;
            var presenter = node.Factory.CreateVirtualNodePresenter(category, name, type, order,
                () => fromProperty(wrapper.GetProjectProperty(propertyName)),
                value => wrapper.SetProjectProperty(propertyName, toProperty(value)),
                hasBase: IsExplicit, // gates the "Reset to base value" menu item
                isInerited: () => !IsExplicit(),
                isOverridden: IsExplicit,
                resetOverride: () => wrapper.SetProjectProperty(propertyName, null));
            presenter.DisplayName = displayName;
            presenter.AttachedProperties.Add(DocumentationData.Key, documentation);
        }
    }
}
