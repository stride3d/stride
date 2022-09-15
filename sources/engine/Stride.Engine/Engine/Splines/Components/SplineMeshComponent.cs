//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Splines.Models;
using Stride.Engine.Splines.Processors;

namespace Stride.Engine.Splines.Components
{
    /// <summary>
    /// Component representing a Spline Traverser.
    /// </summary>
    [DataContract("SplineMeshComponent")]
    [Display("Spline Mesh", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SplineMeshTransformProcessor), ExecutionMode = ExecutionMode.All)]
    [ComponentCategory("Splines")]
    public sealed class SplineMeshComponent : EntityComponent
    {
        public delegate void MeshRequiresUpdate(SplineMeshComponent component);
        public event MeshRequiresUpdate OnMeshRequiresUpdate;

        private SplineComponent splineComponent;
        private SplineMesh splineMesh;

        /// <summary>
        /// Spline mesh
        /// </summary>
        [DataMember(70)]
        [Display("Spline Mesh")]
        public SplineMesh SplineMesh
        {
            get
            {
                return splineMesh;
            }
            set
            {
                splineMesh = value;

                if (splineMesh != null)
                {
                    InvokeMeshRequiresUpdate();
                }
            }
        }

        /// <summary>
        /// The spline to traverse
        /// No spline, no movement
        /// </summary>
        [Display(10, "Spline")]
        public SplineComponent SplineComponent
        {
            get { return splineComponent; }
            set
            {
                var oldValue = splineComponent;
                splineComponent = value;

                if (SplineComponent != null && oldValue != splineComponent)
                {
                    splineComponent.Spline.OnSplineUpdated += InvokeMeshRequiresUpdate;
                    InvokeMeshRequiresUpdate();
                }
            }
        }

        private void InvokeMeshRequiresUpdate()
        {
            SplineMesh.Loop = splineComponent != null && splineComponent.Spline != null && splineComponent.Spline.Loop == true ? true : false;
            OnMeshRequiresUpdate?.Invoke(this);
        }

        internal void Update(TransformComponent transformComponent)
        {

        }

        ~SplineMeshComponent()
        {
            splineMesh = null;
        }
    }
}
