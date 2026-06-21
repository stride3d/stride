// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Engine.InputInteractions;
using Stride.Games;
using Stride.Input;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Game
{
    public class EditorGameCameraOrientationService : EditorGameMouseServiceBase
    {
        private EntityHierarchyEditorGame game;
        private CameraOrientationGizmo gizmo;

        [Obsolete]
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

                    if (gizmo.HasSelection && !InteractionService.HasActiveInteraction)
                    {
                        if (game.Input.IsMouseButtonPressed(MouseButton.Left))
                        {
                            var clickedElement = gizmo.SelectedElement;
                            InteractionService.Request(new InputInteractionRequest
                            {
                                Name = "CameraOrientation.Click",
                                InteractionType = InputInteractionType.Gizmo,
                                Factory = () => new Interaction(this, clickedElement)
                            });
                        }
                    }
                }

                await game.Script.NextFrame();
            }
        }

        private class Interaction(EditorGameCameraOrientationService EditorService, Int3 ClickedElement) : IInputInteraction
        {
            public object Owner => EditorService;

            public void Start()
            {
            }

            public bool Update(GameTime gameTime)
            {
                var game = EditorService.game;
                if (game.Input.IsMouseButtonDown(MouseButton.Left))
                {
                    return true;
                }
                return false;
            }

            public void End()
            {
                var gizmo = EditorService.gizmo;
                var cameraService = EditorService.Camera;
                if (ClickedElement == gizmo.SelectedElement)
                {
                    // Mouse release is still over the same element
                    Int3 selectedElement = ClickedElement;

                    // If looking along a coordinate axis and the corresponding element is clicked, switch projection mode
                    if (gizmo.IsViewParallelToAxis && selectedElement.LengthSquared() == 1)
                    {
                        var camera = cameraService.Camera;
                        camera.Dispatcher.Invoke(() => camera.OrthographicProjection = !camera.OrthographicProjection);
                    }
                    else
                    {
                        var viewDirection = new Vector3(-selectedElement.X, -selectedElement.Y, -selectedElement.Z);
                        cameraService.ResetCamera(viewDirection);
                    }
                }
            }

            public void Cancel()
            {
            }
        }
    }
}
