// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.Services;
using Stride.Editor.Avalonia.Preview.Views;
using Stride.Editor.Preview;

namespace Stride.Assets.Editor.Avalonia;

internal class Module
{
    [Core.ModuleInitializer]
    public static void Initialize()
    {
        AssetsPlugin.RegisterPlugin(typeof(StrideEditorViewPlugin));
        AssetPreview.DefaultViewType = typeof(StridePreviewView);
    }
}
