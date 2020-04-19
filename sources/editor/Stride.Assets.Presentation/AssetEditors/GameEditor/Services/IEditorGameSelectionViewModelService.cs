// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    public interface IEditorGameSelectionViewModelService : IEditorGameViewModelService
    {
        bool DisplaySelectionMask { get; set; }

        void AddSelectable(AbsoluteId id);

        void RemoveSelectable(AbsoluteId id);
    }
}
