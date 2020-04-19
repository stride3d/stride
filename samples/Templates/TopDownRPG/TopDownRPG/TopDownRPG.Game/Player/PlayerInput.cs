// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering;
using TopDownRPG.Core;

namespace TopDownRPG.Player
{
    public class PlayerInput : SyncScript
    {
        /// <summary>
        /// Raised every frame with the intended direction of movement from the player.
        /// </summary>
        public static readonly EventKey<ClickResult> MoveDestinationEventKey = new EventKey<ClickResult>();

        public static readonly EventKey<bool> JumpEventKey = new EventKey<bool>();

        public int ControllerIndex { get; set; }

        public float DeadZone { get; set; } = 0.25f;

        public Entity Highlight { get; set; }

        public Material HighlightMaterial { get; set; }

        public CameraComponent Camera { get; set; }

        public Prefab ClickEffect { get; set; }

        private ClickResult lastClickResult;

        public override void Update()
        {
            if (Input.HasMouse)
            {
                ClickResult clickResult;
                Utils.ScreenPositionToWorldPositionRaycast(Input.MousePosition, Camera, this.GetSimulation(), out clickResult);

                var isMoving = (Input.IsMouseButtonDown(MouseButton.Left) && lastClickResult.Type == ClickType.Ground && clickResult.Type == ClickType.Ground);

                var isHighlit = (!isMoving && clickResult.Type == ClickType.LootCrate);

                // Character continuous moving
                if (isMoving)
                {
                    lastClickResult.WorldPosition = clickResult.WorldPosition;
                    MoveDestinationEventKey.Broadcast(lastClickResult);
                }

                // Object highlighting
                if (isHighlit)
                {
                    var modelComponentA = Highlight?.Get<ModelComponent>();
                    var modelComponentB = clickResult.ClickedEntity.Get<ModelComponent>();

                    if (modelComponentA != null && modelComponentB != null)
                    {
                        var materialCount = modelComponentB.Model.Materials.Count;
                        modelComponentA.Model = modelComponentB.Model;
                        modelComponentA.Materials.Clear();
                        for (int i = 0; i < materialCount; i++)
                            modelComponentA.Materials.Add(i, HighlightMaterial);

                        modelComponentA.Entity.Transform.UseTRS = false;
                        modelComponentA.Entity.Transform.LocalMatrix = modelComponentB.Entity.Transform.WorldMatrix;
                    }
                }
                else
                {
                    var modelComponentA = Highlight?.Get<ModelComponent>();
                    if (modelComponentA != null)
                        modelComponentA.Entity.Transform.LocalMatrix = Matrix.Scaling(0);
                }
            }

            // Mouse-based camera rotation. Only enabled after you click the screen to lock your cursor, pressing escape cancels this
            foreach (var pointerEvent in Input.PointerEvents.Where(x => x.EventType == PointerEventType.Pressed))
            {
                ClickResult clickResult;
                if (Utils.ScreenPositionToWorldPositionRaycast(pointerEvent.Position, Camera, this.GetSimulation(),
                    out clickResult))
                {
                    lastClickResult = clickResult;
                    MoveDestinationEventKey.Broadcast(clickResult);

                    if (ClickEffect != null && clickResult.Type == ClickType.Ground)
                    {
                        this.SpawnPrefabInstance(ClickEffect, null, 1.2f, Matrix.RotationQuaternion(Quaternion.BetweenDirections(Vector3.UnitY, clickResult.HitResult.Normal)) * Matrix.Translation(clickResult.WorldPosition));
                    }
                }
            }
        }
    }
}
