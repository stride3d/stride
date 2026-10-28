// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The camera of the game of an entity hierarchy editor. It opens at the default position of the WPF scene editor,
/// looking at the origin.
/// </summary>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 110)]
public sealed class EditorGameEntityCameraService : EditorGameCameraService
{
    public EditorGameEntityCameraService(IEditorGameController controller)
        : base(controller)
    {
    }

    /// <inheritdoc />
    public override void ResetCamera(Vector3 viewDirection)
    {
        var isViewVertical = MathUtil.NearEqual(viewDirection.X, 0) && MathUtil.NearEqual(viewDirection.Z, 0);
        SetCurrentYaw(isViewVertical ? 0 : MathF.Atan2(-viewDirection.X, -viewDirection.Z));

        var horizontalViewDirection = new Vector2(viewDirection.X, viewDirection.Z);
        SetCurrentPitch(MathF.Atan2(viewDirection.Y, horizontalViewDirection.Length()));
    }

    /// <inheritdoc />
    protected override void Reset()
    {
        base.Reset();

        SetCurrentPitch(DefaultPitch);
        SetCurrentYaw(DefaultYaw);
    }
}
