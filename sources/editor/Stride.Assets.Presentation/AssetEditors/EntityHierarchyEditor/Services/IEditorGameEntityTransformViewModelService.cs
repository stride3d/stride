// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    public interface IEditorGameEntityTransformViewModelService : IEditorGameTransformViewModelService
    {
        double GizmoSize { get; set; }
    }
}
