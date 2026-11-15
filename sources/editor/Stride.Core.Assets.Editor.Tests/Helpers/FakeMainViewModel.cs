// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// The main view model of a test host: the status bar, the Avalonia toolkit, the text asset types and the recent files.
/// </summary>
public sealed class FakeMainViewModel : IMainViewModel
{
    public FakeMainViewModel(IViewModelServiceProvider serviceProvider)
    {
        Status = new StatusViewModel(serviceProvider);
    }

    public StatusViewModel Status { get; }

    public string EditorToolkitName => EditorToolkits.Avalonia;

    /// <summary>
    /// The asset types opened with the shader editor; none by default.
    /// </summary>
    public List<string> TextAssetTypes { get; } = [];

    IEnumerable<string> IMainViewModel.TextAssetTypes => TextAssetTypes;

    /// <summary>
    /// The files added to the recent files, in order.
    /// </summary>
    public List<UFile> RecentFiles { get; } = [];

    public void AddRecentFile(UFile filePath) => RecentFiles.Add(filePath);
}
