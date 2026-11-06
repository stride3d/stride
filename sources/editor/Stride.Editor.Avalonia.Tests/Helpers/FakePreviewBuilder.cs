// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Editor.Avalonia.Preview;
using Stride.Editor.Preview;

namespace Stride.Editor.Avalonia.Tests.Helpers;

/// <summary>
/// A preview builder with no game: it only hands out the control a preview view hosts.
/// </summary>
public sealed class FakePreviewBuilder : IAvaloniaPreviewBuilder
{
    public FakePreviewBuilder(Control? view)
    {
        View = view;
    }

    /// <summary>
    /// The control the preview game would render in.
    /// </summary>
    public Control? View { get; set; }

    public AssetBuilderService AssetBuilderService => throw new NotSupportedException("No builder in this fake.");

    public IDispatcherService Dispatcher => new NullDispatcherService();

    public Logger Logger { get; } = new LoggerResult();

    public PreviewGame PreviewGame => throw new NotSupportedException("No preview game in this fake.");

    public AssetCompilerResult Compile(AssetItem asset) => throw new NotSupportedException("No compilation in this fake.");

    public Control? GetStrideView() => View;
}
