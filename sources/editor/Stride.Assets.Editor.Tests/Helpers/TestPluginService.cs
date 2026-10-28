// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Views;
using Stride.Editor;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// The plugin host of the editors, for the plugins it is given only: it resolves the types they register the way the
/// editor shell's service does, the view model of an asset type (or of a base type) and the view model of a preview type
/// (or of its generic definition). No template provider is taken: there is no view.
/// </summary>
public sealed class TestPluginService : StrideEditorPluginHost
{
    public TestPluginService(params AssetsPlugin[] plugins)
        : base(plugins)
    {
    }

    protected override Type AssetEditorViewModelType => typeof(AssetEditorViewModel);

    protected override Type AssetEditorViewType => typeof(IAssetEditorView);

    public override bool HasEditorView(ISessionViewModel session, Type viewModelType) => GetEditorViewModelType(viewModelType) is { } editor && GetEditorViewType(editor) is not null;

    protected override bool AddTemplateProvider(ISessionViewModel session, ITemplateProvider provider) => false;

    protected override void RemoveTemplateProvider(ISessionViewModel session, ITemplateProvider provider)
    {
    }
}
