// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Input;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The view cube in the corner of the scene view: it shows the orientation of the camera, and a click on a face, an edge
/// or a corner looks at the scene from there. A click on the face the camera already looks at switches between the
/// perspective and the orthographic projections.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>EditorGameCameraOrientationService</c>. The direction names are hidden,
/// as in the default WPF scene editor settings, which are not read yet.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 180)]
public sealed class EditorGameCameraOrientationService : EditorGameMouseServiceBase
{
    private EntityHierarchyEditorGame? game;
    private CameraOrientationGizmo? gizmo;
    private Int3? clickedElement;

    /// <inheritdoc/>
    public override bool IsControllingMouse { get; protected set; }

    /// <inheritdoc/>
    public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameCameraService); } }

    private IEditorGameCameraService Camera => Services.Get<IEditorGameCameraService>();

    private EntityHierarchyEditorGame Game => game ?? throw new InvalidOperationException("The service is not initialized.");

    private CameraOrientationGizmo Gizmo => gizmo ?? throw new InvalidOperationException("The service is not initialized.");

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        EnsureNotDestroyed(nameof(EditorGameCameraOrientationService));

        gizmo?.Dispose();
        return base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = (EntityHierarchyEditorGame)editorGame;
        gizmo = new CameraOrientationGizmo(Camera, game);
        gizmo.Initialize(game.Services, game.EditorScene);

        game.Script.AddTask(Update);
        return Task.FromResult(true);
    }

    private async Task Update()
    {
        while (!IsDisposed)
        {
            if (IsActive)
            {
                Gizmo.Update();

                if (Gizmo.HasSelection && IsMouseAvailable)
                {
                    IsControllingMouse = true;
                    if (Game.Input.IsMouseButtonPressed(MouseButton.Left))
                    {
                        clickedElement = Gizmo.SelectedElement;
                    }
                    // A quick click can press and release the button in the same frame
                    if (Game.Input.IsMouseButtonReleased(MouseButton.Left))
                    {
                        if (clickedElement == Gizmo.SelectedElement)
                            LookFrom(Gizmo.SelectedElement);

                        clickedElement = null;
                        IsControllingMouse = false;
                    }
                }
                else
                {
                    IsControllingMouse = false;
                }
            }

            await Game.Script.NextFrame();
        }
    }

    /// <summary>
    /// Looks at the scene from the clicked face, edge or corner of the cube.
    /// </summary>
    private void LookFrom(Int3 element)
    {
        // Looking along an axis, a click on its face switches the projection
        if (Gizmo.IsViewParallelToAxis && element.LengthSquared() == 1)
        {
            Camera.IsOrthographic = !Camera.IsOrthographic;
        }
        else
        {
            Camera.ResetCamera(new Vector3(-element.X, -element.Y, -element.Z));
        }
    }
}
