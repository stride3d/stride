// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace SpaceEscape.Background
{
    /// <summary>
    /// Represents a section of the background which contains other game objects.
    /// </summary>
    public class Section
    {
        /// <summary>
        /// Gets the length of the level block
        /// </summary>
        public float Length { get; private set; }

        /// <summary>
        /// Gets the root entity of the level block
        /// </summary>
        public Entity RootEntity { get; private set; }

        /// <summary>
        /// Gets the list collidable obstacles of the level block
        /// </summary>
        public List<Obstacle> CollidableObstacles { get; private set; }

        /// <summary>
        /// Gets the list of holes of the level block
        /// </summary>
        public List<Hole> Holes { get; private set; }

        /// <summary>
        /// Create a new empty level block.
        /// </summary>
        public Section()
        {
            RootEntity = new Entity();
            CollidableObstacles = new List<Obstacle>();
            Holes = new List<Hole>();
        }

        /// <summary>
        /// Gets the distance in the Oz axis of the level block to the origin.
        /// </summary>
        public float PositionZ
        {
            get { return RootEntity.Transform.Position.Z; }
            set { RootEntity.Transform.Position.Z = value; }
        }

        public Section AddBackgroundEntity(Entity backgroundEntity)
        {
            // Attach  it in ModelEntity
            RootEntity.AddChild(backgroundEntity);

            // Get length via its bounding box
            var modelComponent = backgroundEntity.Get<ModelComponent>().Model;
            var boundingBox = modelComponent.BoundingBox;

            Length += boundingBox.Maximum.Z - boundingBox.Minimum.Z;

            return this;
        }

        /// <summary>
        /// Chaining method for adding an obstacle to this Section.
        /// It initializes bounding boxes and stores in Collidable Obstacles.
        /// </summary>
        /// <param name="obstacleEntity">The entity containing the obstacle</param>
        /// <param name="useSubBoundingBoxes">true to use the bounding boxes of the sub-meshes</param>
        /// <returns></returns>
        public Section AddObstacleEntity(Entity obstacleEntity, bool useSubBoundingBoxes)
        {
            // Attach it in ModelEntity
            RootEntity.AddChild(obstacleEntity);

            // Get and add bb to CollidableObstacles
            var modelComponent = obstacleEntity.Get<ModelComponent>();

            var collidableObstacle = new Obstacle { Entity = obstacleEntity };

            if (useSubBoundingBoxes)
            {
                // Use bounding boxes from parts of the obstacle.
                foreach (var mesh in modelComponent.Model.Meshes)
                {
                    var boundingBox = mesh.BoundingBox;
                    var nodeIndex = mesh.NodeIndex;
                    while (nodeIndex >= 0)
                    {
                        var node = modelComponent.Model.Skeleton.Nodes[nodeIndex];
                        var transform = node.Transform;
                        var matrix = Matrix.Transformation(Vector3.Zero, Quaternion.Identity, transform.Scale, Vector3.Zero, transform.Rotation, transform.Position);

                        Vector3.TransformNormal(ref boundingBox.Minimum, ref matrix, out boundingBox.Minimum);
                        Vector3.TransformNormal(ref boundingBox.Maximum, ref matrix, out boundingBox.Maximum);

                        nodeIndex = node.ParentIndex;
                    }

                    collidableObstacle.BoundingBoxes.Add(boundingBox);
                }
            }
            else
            {
                // Use bounding box of the whole model
                collidableObstacle.BoundingBoxes.Add(modelComponent.Model.BoundingBox); 
            }

            CollidableObstacles.Add(collidableObstacle);

            return this;
        }

        /// <summary>
        /// Add list of Holes to this Section.
        /// </summary>
        /// <param name="holes"></param>
        /// <returns></returns>
        public Section AddHoleRange(List<Hole> holes)
        {
            if(holes != null)
                Holes.AddRange(holes);

            return this;
        }
    }
}
