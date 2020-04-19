// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Engine;
using SpaceEscape.Effects;
using System.Linq;
using Stride.Core.Extensions;

namespace SpaceEscape.Background
{
    /// <summary>
    /// BackgroundScript controls background section in the game.
    /// </summary>
    public class BackgroundScript : SyncScript
    {
        public event Action<float> DistanceUpdated;

        private const float GameSpeed = 30f;
        private const float RemoveBlockPosition = -16f;
        private const float AddBlockPosition = 280f;
        private const int NumberOfStartBlock = 5;

        public Scene LevelBlocks;
        public Model SkyplaneModel; // Cache to scroll its UV region.

        internal static readonly Random Random = new Random();
        private readonly List<Section> levelBlocks = new List<Section>();
        private LevelGenerator levelGenerator;
        private float runningDistance; // Store how far the player progressed in m.

        private static readonly Vector3 SkyPlanePosition = new Vector3(0, -10f, 340f);
        private Vector4 skyplaneUVRegion = new Vector4(0f, 0f, 1f, 1f);
        private bool isScrolling;
        private Entity skyplaneEntity;

        private float RunningDistance
        {
            get{ return runningDistance; }
            set
            {
                runningDistance = value;
                if (DistanceUpdated != null)
                    DistanceUpdated(runningDistance);
            }
        }

        public override void Start()
        {
            var levelBlockEntity = LevelBlocks.Entities.First(x => x.Name == "LevelBlocks");
            levelGenerator = levelBlockEntity.Get<LevelGenerator>();

            RunningDistance = 0f;

            // Load SkyPlane
            skyplaneEntity = new Entity { new ModelComponent(SkyplaneModel) };

            skyplaneEntity.Transform.Position= SkyPlanePosition;
            SkyplaneModel.Meshes[0].Parameters.Set(GameParameters.EnableBend, false);
            SkyplaneModel.Meshes[0].Parameters.Set(GameParameters.EnableOnflyTextureUVChange, true);

            // Add skyPlane with LevelBlocks to EntitySystem
            SceneSystem.SceneInstance.RootScene.Entities.Add(skyplaneEntity);
            CreateStartLevelBlocks();
        }

        public override void Cancel()
        {
            Entity.Transform.Children.Clear();
            SceneSystem.SceneInstance.RootScene.Entities.Remove(skyplaneEntity);
        }

        public override void Update()
        {
            if (!isScrolling)
                return;

            var elapsedTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            // Check if needed to remove the first block
            var firstBlock = levelBlocks[0];
            if (firstBlock.PositionZ + firstBlock.Length * 0.5f < RemoveBlockPosition)
            {
                RemoveLevelBlock(firstBlock);
            }

            // Check if needed to add new levelblock
            var lastBlock = levelBlocks[levelBlocks.Count - 1];
            if (lastBlock.PositionZ - lastBlock.Length * 0.5f < AddBlockPosition)
            {
                AddLevelBlock(levelGenerator.RandomCreateLevelBlock());
            }

            // Move levelblocks
            foreach (var levelBlock in levelBlocks)
            {
                var moveDist = GameSpeed * elapsedTime;
                levelBlock.PositionZ -= moveDist;
                RunningDistance += moveDist / 100f;
            }

            if (skyplaneUVRegion.X < -1f)
                // Reset scrolling position of Skyplane's UV
                skyplaneUVRegion.X = 0f;

            // Move Scroll position by an offset every frame.
            skyplaneUVRegion.X -= 0.0005f;

            // Update Parameters of the shader
            SkyplaneModel.Meshes[0].Parameters.Set(TransformationTextureUVKeys.TextureRegion, skyplaneUVRegion);
        }

        public void Reset()
        {
            RunningDistance = 0f;
            isScrolling = false;

            for (var i = levelBlocks.Count - 1; i >= 0; i--)
            {
                var levelBlock = levelBlocks[i];

                Entity.RemoveChild(levelBlock.RootEntity);
                levelBlocks.RemoveAt(i);
            }

            CreateStartLevelBlocks();
        }

        public bool DetectCollisions(ref BoundingBox agentBB)
        {
            foreach (var levelBlock in levelBlocks)
            {
                foreach (var obst in levelBlock.CollidableObstacles)
                {
                    if (DetectCollision(ref agentBB, obst))
                        return true;
                }
            }

            return false;
        }

        private static bool DetectCollision(ref BoundingBox agentBB,
            Obstacle obst)
        {
            var objTrans = obst.Entity.Transform;
            objTrans.UpdateWorldMatrix();

            var objWorldPos = objTrans.WorldMatrix.TranslationVector;

            foreach (var boundingBox in obst.BoundingBoxes)
            {
                var minVec = objWorldPos + boundingBox.Minimum;
                var maxVec = objWorldPos + boundingBox.Maximum;
                var testBB = new BoundingBox(minVec, maxVec);

                if (CollisionHelper.BoxContainsBox(ref testBB, ref agentBB) != ContainmentType.Disjoint)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Starts the scrolling of the background
        /// </summary>
        public void StartScrolling()
        {
            isScrolling = true;
        }

        /// <summary>
        /// Stops the scrolling of the background
        /// </summary>
        public void StopScrolling()
        {
            isScrolling = false;
        }

        public bool DetectHoles(ref Vector3 agentWorldPos, out float height)
        {
            height = 0f;

            foreach (var levelBlock in levelBlocks)
            {
                levelBlock.RootEntity.Transform.UpdateWorldMatrix();
                var worldPosZ = levelBlock.RootEntity.Transform.Position.Z;
                foreach (var hole in levelBlock.Holes)
                {
                    if (DetectHole(ref agentWorldPos, out height, hole,
                        worldPosZ))
                        return true;
                }
            }
            return false;
        }

        private static bool DetectHole(ref Vector3 agentWorldPos, out float height, Hole hole, float blockPosZ)
        {
            var testArea = hole.Area;
            testArea.Y += blockPosZ;
            height = hole.Height;

            var agentVec2Pos = new Vector2(-agentWorldPos.X, agentWorldPos.Z);
            return RectContains(ref testArea, ref agentVec2Pos);
        }

        private void CreateStartLevelBlocks()
        {
            AddLevelBlock(levelGenerator.CreateSafeLevelBlock());

            for (var i = 0; i < NumberOfStartBlock; i++)
                AddLevelBlock(levelGenerator.RandomCreateLevelBlock());
        }

        private void AddLevelBlock(Section newSection)
        {
            var count = levelBlocks.Count;
            levelBlocks.Add(newSection);

            if (count == 0)
            {
                Entity.AddChild(newSection.RootEntity);
                return;
            }

            var prevLatestBlock = levelBlocks[count - 1];

            var originDist = 0.5f * (prevLatestBlock.Length + newSection.Length);
            newSection.PositionZ = prevLatestBlock.PositionZ + originDist;

            Entity.AddChild(newSection.RootEntity);
        }

        private void RemoveLevelBlock(Section firstBlock)
        {
            levelBlocks.Remove(firstBlock);
            Entity.RemoveChild(firstBlock.RootEntity);
        }

        private static bool RectContains(ref RectangleF rect, ref Vector2 agentPos)
        {
            return (rect.X <= agentPos.X) && (rect.X + rect.Width >= agentPos.X)
                   && (rect.Y >= agentPos.Y) && (rect.Y - rect.Height <= agentPos.Y);
        }
    }
}
