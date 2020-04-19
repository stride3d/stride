// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Editor.EditorGame.Game;
using Stride.Input;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Game
{
    public class EditorGameCameraOrientationService : EditorGameMouseServiceBase
    {
        private EntityHierarchyEditorGame game;
        private CameraOrientationGizmo gizmo;
        private Int3? clickedElement;

        public override bool IsControllingMouse { get; protected set; }

        public override IEnumerable<Type> Dependencies { get { yield return typeof(EditorGameEntityCameraService); } }
        
        internal EditorGameEntityCameraService Camera => Services.Get<EditorGameEntityCameraService>();

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            game = (EntityHierarchyEditorGame)editorGame;
            gizmo = new CameraOrientationGizmo(this, game);
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
                    gizmo.Update();

                    if (gizmo.HasSelection && IsMouseAvailable)
                    {
                        IsControllingMouse = true;
                        if (game.Input.IsMouseButtonPressed(MouseButton.Left))
                        {
                            clickedElement = gizmo.SelectedElement;
                        }
                        else if (game.Input.IsMouseButtonReleased(MouseButton.Left))
                        {
                            if (clickedElement.HasValue && clickedElement == gizmo.SelectedElement)
                            {
                                Int3 selectedElement = clickedElement.Value;

                                // If looking along a coordinate axis and the corresponding element is clicked, switch projection mode
                                if (gizmo.IsViewParallelToAxis && selectedElement.LengthSquared() == 1)
                                {
                                    var camera = Camera.Camera;
                                    camera.Dispatcher.Invoke(() => camera.OrthographicProjection = !camera.OrthographicProjection);
                                }
                                else
                                {
                                    var viewDirection = new Vector3(-selectedElement.X, -selectedElement.Y, -selectedElement.Z);
                                    Camera.ResetCamera(viewDirection);
                                }
                            }

                            clickedElement = null;
                            IsControllingMouse = false;
                        }
                    }
                    else
                    {
                        IsControllingMouse = false;
                    }
                }

                await game.Script.NextFrame();
            }
        }
    }
}
