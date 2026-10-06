// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// The cursor shown over the editor game, named without a UI toolkit (see <see cref="EditorGameController.ChangeCursor"/>).
    /// </summary>
    public enum EditorGameCursor
    {
        Default,
        No,
        SizeAll,
        SizeNS,
        SizeWE,
        SizeNWSE,
        SizeNESW,
    }
}
