// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace SpaceEscape.Background
{

    public class LevelGenerator : ScriptComponent
    {
        public Entity Background_a00;
        public Entity Background_b00;
        public Entity Background_b01;
        public Entity Background_b02;
        public Entity Background_b03;
        public Entity Background_b04;

        public Entity Obstacle1;
        public Entity Obstacle2;

        /// <summary>
        /// Randomly create pattern from defined Factory method of each one.
        /// </summary>
        /// <returns></returns>
        public Section RandomCreateLevelBlock()
        {
            Entity backgroundEntity;
            switch (BackgroundScript.Random.Next(6))
            {
                default:
                case 0: backgroundEntity = Background_a00; break;
                case 1: backgroundEntity = Background_b00; break;
                case 2: backgroundEntity = Background_b01; break;
                case 3: backgroundEntity = Background_b02; break;
                case 4: backgroundEntity = Background_b03; break;
                case 5: backgroundEntity = Background_b04; break;
            }

            return CreateLevelBlock(backgroundEntity.Clone());
        }

        public Section CreateSafeLevelBlock()
        {
            return CreateLevelBlock(Background_a00.Clone(), 0);
        }

        /// <summary>
        /// Randomly get Obstacle from available keys and return the clone.
        /// </summary>
        /// <returns></returns>
        private Entity CloneRandomObstacle(out bool useSubmeshesBoundingBoxes)
        {
            Entity obstacleEntity;
            switch (BackgroundScript.Random.Next(2))
            {
                default:
                case 0: obstacleEntity = Obstacle1; break;
                case 1: obstacleEntity = Obstacle2; break;
            }

            useSubmeshesBoundingBoxes = obstacleEntity.Get<ObstacleInfo>().UseSubMeshBoundingBoxes;
            obstacleEntity = obstacleEntity.Clone();

            // Reset position
            obstacleEntity.Transform.Position = Vector3.Zero;

            return obstacleEntity;
        }

        /// <summary>
        /// Factory method to create Section from a given BackgroundEntity
        /// </summary>
        /// <param name="backgroundEnt"></param>
        /// <param name="maxObstacleOverride">The maximum number of obstacle in the block level</param>
        /// <returns></returns>
        private Section CreateLevelBlock(Entity backgroundEnt, int? maxObstacleOverride = null)
        {
            // Reset position
            backgroundEnt.Transform.Position = Vector3.Zero;

            var levelBlock = new Section();
            var backgroundInfo = backgroundEnt.Get<BackgroundInfo>();
            levelBlock.AddBackgroundEntity(backgroundEnt).AddHoleRange(backgroundInfo.Holes);

            var len = levelBlock.Length;
            RandomAddObstacles(levelBlock, len, maxObstacleOverride ?? backgroundInfo.MaxNbObstacles);

            return levelBlock;
        }

        /// <summary>
        /// Randomly add obstacles to the given level block, while
        /// the number of obstacles to be added is from nbObst.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="patternLen"></param>
        /// <param name="nbObst"></param>
        private void RandomAddObstacles(Section section, float patternLen, int nbObst)
        {
            var halfPatternLen = patternLen / 2f;

            for (var i = 0; i < nbObst; i++)
            {
                // Random val in {0.0-1.0}
                var randVal = BackgroundScript.Random.NextDouble();

                // Calculate position in Z axis by:
                // changing the value in uniform space (randVal) to world space.
                // the value is substracted with halfPatternLen because the origin (0) is at the center of the block.
                var posZ = patternLen * (float)((i + randVal) / nbObst) - halfPatternLen;

                // Random lane, and get the world position in X axis.
                var lane = BackgroundScript.Random.Next(3);
                var posX = (1 - lane) * 5f;

                // Randomly get the obstacle, and set the position of this obstacle.
                bool useSubBoundingBoxes;
                var obsEnt = CloneRandomObstacle(out useSubBoundingBoxes);
                obsEnt.Transform.Position = new Vector3(posX, 0, posZ);

                section.AddObstacleEntity(obsEnt, useSubBoundingBoxes);
            }
        }
    }
}
