// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Assets.Editor.Avalonia.Templates;
using Stride.Core.Assets.Editor.Services;
using Stride.Editor.Avalonia.Preview.Views;
using Stride.Editor.Preview;

namespace Stride.Assets.Editor.Avalonia;

internal static class Module
{
    [Core.ModuleInitializer]
    public static void Initialize()
    {
        // Register the toolkit-neutral editor layer before its views, so its plugin comes first
        RuntimeHelpers.RunModuleConstructor(typeof(Stride.Assets.Editor.Module).Module.ModuleHandle);
        AssetsPlugin.RegisterPlugin(typeof(StrideEditorViewPlugin));
        AssetPreview.DefaultViewType = typeof(StridePreviewView);
        // The project template generators, with this toolkit's prompts
        StrideAvaloniaTemplates.Register();
    }
}
