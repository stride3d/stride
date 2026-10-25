// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<ScriptSourceFileViewModel>]
public sealed class ScriptSourceFileEditorViewModel : AssetEditorViewModel<ScriptSourceFileViewModel>
{
    public ScriptSourceFileEditorViewModel(ScriptSourceFileViewModel asset)
        : base(asset)
    {
    }
}
