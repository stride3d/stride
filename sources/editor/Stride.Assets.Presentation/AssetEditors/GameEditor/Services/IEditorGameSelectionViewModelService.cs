// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Editor.EditorGame.ViewModels;

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor.Services
{
    public interface IEditorGameSelectionViewModelService : IEditorGameViewModelService
    {
        bool DisplaySelectionMask { get; set; }

        void AddSelectable(AbsoluteId id);

        void RemoveSelectable(AbsoluteId id);
    }
}
