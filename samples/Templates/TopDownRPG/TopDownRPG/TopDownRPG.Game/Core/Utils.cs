// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;

namespace TopDownRPG.Core
{
    public static class Utils
    {
        public static void SpawnPrefabModel(this ScriptComponent script, Prefab source, Entity attachEntity, Matrix localMatrix, Vector3 forceImpulse)
        {
            if (source == null)
                return;

            // Clone
            var spawnedEntities = source.Instantiate();

            // Add
            foreach (var prefabEntity in spawnedEntities)
            {
                prefabEntity.Transform.UpdateLocalMatrix();
                var entityMatrix = prefabEntity.Transform.LocalMatrix * localMatrix;
                entityMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation, out prefabEntity.Transform.Position);

                if (attachEntity != null)
                {
                    attachEntity.AddChild(prefabEntity);
                }
                else
                {
                    script.SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
                }

                var physComp = prefabEntity.Get<RigidbodyComponent>();
                if (physComp != null)
                {
                    physComp.ApplyImpulse(forceImpulse);
                }
            }
        }

        public static void SpawnPrefabInstance(this ScriptComponent script, Prefab source, Entity attachEntity, float timeout, Matrix localMatrix)
        {
            if (source == null)
                return;

            Func<Task> spawnTask = async () =>
            {
                // Clone
                var spawnedEntities = source.Instantiate();

                // Add
                foreach (var prefabEntity in spawnedEntities)
                {
                    prefabEntity.Transform.UpdateLocalMatrix();
                    var entityMatrix = prefabEntity.Transform.LocalMatrix * localMatrix;
                    entityMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation, out prefabEntity.Transform.Position);

                    if (attachEntity != null)
                    {
                        attachEntity.AddChild(prefabEntity);
                    }
                    else
                    {
                        script.SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
                    }
                }

                // Countdown
                var secondsCountdown = timeout;
                while (secondsCountdown > 0f)
                {
                    await script.Script.NextFrame();
                    secondsCountdown -= (float)script.Game.UpdateTime.Elapsed.TotalSeconds;
                }

                // Remove
                foreach (var clonedEntity in spawnedEntities)
                {
                    if (attachEntity != null)
                    {
                        attachEntity.RemoveChild(clonedEntity);
                    }
                    else
                    {
                        script.SceneSystem.SceneInstance.RootScene.Entities.Remove(clonedEntity);
                    }
                }

                // Cleanup
                spawnedEntities.Clear();
            };

            script.Script.AddTask(spawnTask);
        }

        /// <summary>
        /// Removes an entity, together with its children, from the Game's scene graph
        /// </summary>
        /// <param name="game">The game instance containing the entity</param>
        /// <param name="entity">Entity to remove</param>
        public static void RemoveEntity(this IGame game, Entity entity)
        {
            var parent = entity.GetParent();
            if (parent != null)
            {
                parent.RemoveChild(entity);
                return;
            }

            ((Game)game).SceneSystem.SceneInstance.RootScene.Entities.Remove(entity);
        }

        public static async Task WaitTime(this IGame game, TimeSpan time)
        {
            var g = (Game)game;
            var goal = game.UpdateTime.Total + time;
            while (game.UpdateTime.Total < goal)
            {
                await g.Script.NextFrame();
            }
        }

        public static Vector3 LogicDirectionToWorldDirection(Vector2 logicDirection, CameraComponent camera, Vector3 upVector)
        {
            var inverseView = Matrix.Invert(camera.ViewMatrix);

            var forward = Vector3.Cross(upVector, inverseView.Right);
            forward.Normalize();

            var right = Vector3.Cross(forward, upVector);
            var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
            worldDirection.Normalize();
            return worldDirection;
        }

        public static bool ScreenPositionToWorldPositionRaycast(Vector2 screenPos, CameraComponent camera, Simulation simulation, out ClickResult clickResult)
        {
            Matrix invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);

            Vector3 sPos;
            sPos.X = screenPos.X * 2f - 1f;
            sPos.Y = 1f - screenPos.Y * 2f;

            sPos.Z = 0f;
            var vectorNear = Vector3.Transform(sPos, invViewProj);
            vectorNear /= vectorNear.W;

            sPos.Z = 1f;
            var vectorFar = Vector3.Transform(sPos, invViewProj);
            vectorFar /= vectorFar.W;

            clickResult.ClickedEntity = null;
            clickResult.WorldPosition = Vector3.Zero;
            clickResult.Type = ClickType.Empty;
            clickResult.HitResult = new HitResult();

            var minDistance = float.PositiveInfinity;

            var result = new FastList<HitResult>();
            simulation.RaycastPenetrating(vectorNear.XYZ(), vectorFar.XYZ(), result);
            foreach (var hitResult in result)
            {
                ClickType type = ClickType.Empty;
                
                var staticBody = hitResult.Collider as StaticColliderComponent;
                if (staticBody != null)
                {
                    if (staticBody.CollisionGroup == CollisionFilterGroups.CustomFilter1)
                        type = ClickType.Ground;

                    if (staticBody.CollisionGroup == CollisionFilterGroups.CustomFilter2)
                        type = ClickType.LootCrate;

                    if (type != ClickType.Empty)
                    {
                        var distance = (vectorNear.XYZ() - hitResult.Point).LengthSquared();
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            clickResult.Type = type;
                            clickResult.HitResult = hitResult;
                            clickResult.WorldPosition = hitResult.Point;
                            clickResult.ClickedEntity = hitResult.Collider.Entity;
                        }
                    }
                }
            }

            return (clickResult.Type != ClickType.Empty);
        }
    }
}
