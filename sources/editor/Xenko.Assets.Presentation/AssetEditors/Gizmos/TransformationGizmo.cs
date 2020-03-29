// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Xenko.Assets.Presentation.AssetEditors.GameEditor;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Game;
using Xenko.Engine;
using Xenko.Engine.Processors;
using Xenko.Graphics;
using Xenko.Input;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// Base class for all gizmo that applies a transformation on entity
    /// </summary>
    public abstract class TransformationGizmo : AxialGizmo
    {
        public const float TransformationStartPixelThreshold = 8;

        protected const float MinimumRayAngle = 2.5f * MathUtil.Pi / 180f;

        protected struct InitialTransformation
        {
            public Vector3 Translation;
            public Vector3 Scale;
            public Quaternion Rotation;
            public Matrix InverseParentMatrix;

            public bool IsIdentity()
            {
                return Translation == Vector3.Zero && Rotation == Quaternion.Identity && Scale == Vector3.One;
            }
        }

        public const RenderGroup TransformationGizmoGroup = RenderGroup.Group4;

        public const RenderGroupMask TransformationGizmoGroupMask = RenderGroupMask.Group4;

        private bool transformationInitialized;
        private bool transformationStarted;
        private bool duplicationDone;

        /// <summary>
        /// Event triggered when a gizmo transformation finishes.
        /// </summary>
        public event EventHandler TransformationEnded;

        /// <summary>
        /// Gets the gizmo default scale in ratio of screen height ( 1 => full screen vertically )
        /// </summary>
        public float DefaultScale => GizmoDefaultSize / GraphicsDevice.Presenter.BackBuffer.Height;

        /// <summary>
        /// The default material for the origin elements
        /// </summary>
        protected Material DefaultOriginMaterial;

        /// <summary>
        /// The default material for a selected element
        /// </summary>
        protected Material ElementSelectedMaterial;

        /// <summary>
        /// The default material for a transparent selected element
        /// </summary>
        protected Material TransparentElementSelectedMaterial;

        /// <summary>
        /// The transformations of the selected entities at the beginning of the transformations.
        /// </summary>
        protected Dictionary<Entity, InitialTransformation> InitialTransformations = new Dictionary<Entity, InitialTransformation>();

        /// <summary>
        /// The position of the first click of the transformation on the screen.
        /// </summary>
        protected Vector2 StartMousePosition;

        /// <summary>
        /// The value of the gizmo world matrix at the beginning of the transformation.
        /// </summary>
        protected Matrix StartWorldMatrix = Matrix.Identity;
        
        /// <summary>
        /// The projection plane of the transformation.
        /// </summary>
        protected Plane ProjectionPlane;

        /// <summary>
        /// The position of the first click on the projection plane (world space)
        /// </summary>
        protected Vector3 StartClickPoint;

        /// <summary>
        /// The direction of the transformation on the screen
        /// </summary>
        protected Vector2 TransformationDirection;

        /// <summary>
        /// Gets or sets the snap value.
        /// </summary>
        public float SnapValue { get; set; }

        /// <summary>
        /// Gets or sets whether to snap entities using the <see cref="SnapValue"/>.
        /// </summary>
        public bool UseSnap { get; set; }

        /// <summary>
        /// Gets or sets the working space of the gizmo.
        /// </summary>
        public TransformationSpace Space { get; set; }

        /// <summary>
        /// Gets or sets the entity with is modified by the <see cref="TransformationGizmo"/>
        /// </summary>
        public Entity AnchorEntity { get; set; }

        /// <summary>
        /// Gets or sets the entity modified by the gizmo.
        /// </summary>
        public IReadOnlyCollection<Entity> ModifiedEntities { get; set; }
        
        protected TransformationGizmo()
        {
            RenderGroup = TransformationGizmoGroup;
        }

        protected override Entity Create()
        {
            base.Create();
            
            DefaultOriginMaterial = CreateUniformColorMaterial(Color.White);
            ElementSelectedMaterial = CreateUniformColorMaterial(Color.Gold);
            TransparentElementSelectedMaterial = CreateUniformColorMaterial(Color.Gold.WithAlpha(86));

            return null;
        }

        /// <summary>
        /// Gets the gizmo transformation axes.
        /// </summary>
        public GizmoTransformationAxes TransformationAxes { get; protected set; }

        public override bool IsUnderMouse(int pickedComponentId)
        {
            return IsUnderMouse();
        }
        
        public bool IsUnderMouse()
        {
            return TransformationAxes != GizmoTransformationAxes.None;
        }
        
        /// <summary>
        /// Gets the world matrix of the gizmo
        /// </summary>
        protected Matrix WorldMatrix => GizmoRootEntity.Transform.WorldMatrix;

        protected static void UpdateSelectionOnCloserIntersection(BoundingBox box, Ray clickRay, GizmoTransformationAxes axes, ref float minHitDistance, ref GizmoTransformationAxes newSelection)
        {
            float hitDistance;
            if (box.Intersects(ref clickRay, out hitDistance) && hitDistance < minHitDistance)
            {
                minHitDistance = hitDistance;
                newSelection = axes;
            }
        }

        private Matrix GetWorldMatrix(IEditorGameCameraService cameraService)
        {
            Matrix worldMatrix = Matrix.Identity;

            switch (Space)
            {
                case TransformationSpace.WorldSpace:
                    worldMatrix.TranslationVector = AnchorEntity.Transform.WorldMatrix.TranslationVector;
                    break;
                case TransformationSpace.ObjectSpace:
                    var parentMatrix = Matrix.Identity;
                    if (AnchorEntity.GetParent() != null)
                        parentMatrix = AnchorEntity.TransformValue.Parent.WorldMatrix;

                    // We don't use the entity's "WorldMatrix" because it's scale could be zero, which would break the gizmo.
                    worldMatrix = Matrix.RotationQuaternion(AnchorEntity.Transform.Rotation) *
                                  Matrix.Translation(AnchorEntity.Transform.Position) *
                                  parentMatrix;
                    break;
                case TransformationSpace.ViewSpace:
                    worldMatrix = Matrix.Invert(cameraService.ViewMatrix);
                    worldMatrix.TranslationVector = AnchorEntity.Transform.WorldMatrix.TranslationVector;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return worldMatrix;
        }

        private float GetTargetedScale(IEditorGameCameraService cameraService)
        {
            if (cameraService.Component.Projection == CameraProjectionMode.Perspective)
            {
                var distanceToSelectedEntity = Math.Abs(Vector3.TransformCoordinate(AnchorEntity.Transform.WorldMatrix.TranslationVector, cameraService.ViewMatrix).Z);
                return SizeFactor * DefaultScale * 2f * (float)Math.Tan(MathUtil.DegreesToRadians(cameraService.VerticalFieldOfView / 2)) * distanceToSelectedEntity;
            }

            return SizeFactor * DefaultScale * cameraService.Component.OrthographicSize;
        }

        protected virtual void UpdateTransformation()
        {
            if (AnchorEntity == null)
                return;

            // force to recalculate the entity world matrix to avoid the gizmo to have one frame of delay.
            AnchorEntity.Transform.UpdateWorldMatrix(); // TODO perform this computation only when necessary?

            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();

            Matrix worldMatrix = GetWorldMatrix(cameraService);
            float targetedScale = GetTargetedScale(cameraService);

            // Now scale the matrix so the gizmo always has the same on-screen size:
            var worldMatrixRow1 = worldMatrix.Row1;
            var worldMatrixRow2 = worldMatrix.Row2;
            var worldMatrixRow3 = worldMatrix.Row3;

            worldMatrixRow1 *= targetedScale / worldMatrixRow1.Length();    // Normalize the axes and scale them by "targetedScale".
            worldMatrixRow2 *= targetedScale / worldMatrixRow2.Length();
            worldMatrixRow3 *= targetedScale / worldMatrixRow3.Length();

            worldMatrix.Row1 = worldMatrixRow1;
            worldMatrix.Row2 = worldMatrixRow2;
            worldMatrix.Row3 = worldMatrixRow3;

            GizmoRootEntity.Transform.UseTRS = false;
            GizmoRootEntity.Transform.LocalMatrix = worldMatrix;
            GizmoRootEntity.Transform.UpdateWorldMatrix();
        }

        private void UpdateTransformationAxisBase()
        {
            if (Game.EditorServices.Get<IEditorGameCameraService>().IsMoving)
                return;

            if (duplicationDone)
                return;

            UpdateTransformationAxis();
        }

        protected abstract void UpdateTransformationAxis();

        protected abstract InitialTransformation CalculateTransformation();

        protected virtual void OnTransformationFinished()
        {
            duplicationDone = false;
            if (InitialTransformations.Count > 0)
            {
                InitialTransformations.Clear();
                TransformationEnded?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Initialize a new transformation on the selected entities.
        /// </summary>
        protected virtual void InitializeTransformation()
        {
            StartMousePosition = Input.MousePosition;
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();

            // calculate the un-projection plane for 2D transformations
            var planeNormal = Vector3.Zero;
            StartWorldMatrix = WorldMatrix;
            var gizmoViewInverse = Matrix.Invert(StartWorldMatrix * cameraService.ViewMatrix);
            if (EditorGameComponentGizmoService.PlaneToIndex.ContainsKey(TransformationAxes))
            {
                planeNormal[EditorGameComponentGizmoService.PlaneToIndex[TransformationAxes]] = 1f;
            }
            else if (TransformationAxes == GizmoTransformationAxes.XYZ)
            {
                planeNormal = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, gizmoViewInverse));
            }
            else
            {
                var axisVector = Vector3.Zero;
                for (int i = 0; i < 3; i++)
                {
                    if (((int)TransformationAxes & (1 << i)) != 0)
                        axisVector[i] = 1;
                }
                var cameraVector = (Vector3)gizmoViewInverse.Row3;
                var planeVector = Vector3.Normalize(Vector3.Cross(axisVector, cameraVector));
                planeNormal = Vector3.Cross(planeVector, axisVector);

                //This is a temporary fix for weird rotation behavior, it's not working for translation tho
                if (MathUtil.NearEqual(Math.Abs(Vector3.Dot(axisVector, Vector3.Normalize(cameraVector))), 1.0f))
                {
                    planeNormal = axisVector;
                }
            }
            ProjectionPlane = new Plane(Vector3.Zero, planeNormal);

            // determine the position of the start click in the world space
            var ray = EditorGameHelper.CalculateRayFromMousePosition(cameraService.Component, StartMousePosition, gizmoViewInverse);
            transformationInitialized = ProjectionPlane.Intersects(ref ray, out StartClickPoint);
        }

        protected virtual void OnTransformationStarted(Vector2 mouseDragPixel)
        {
            transformationStarted = true;

            // keep in memory all initial transformation states
            InitialTransformations.Clear();
            foreach (var entity in ModifiedEntities)
            {
                // Ensure world matrix is computed
                entity.Transform.UpdateWorldMatrix();

                InitialTransformations[entity] = new InitialTransformation
                {
                    Scale = entity.Transform.Scale,
                    Translation = entity.Transform.Position,
                    Rotation = entity.Transform.Rotation,
                    InverseParentMatrix = entity.Transform.Parent != null ? Matrix.Invert(entity.Transform.Parent.WorldMatrix) : Matrix.Identity
                };
            }
        }

        /// <summary>
        /// Update the transformation of the selected entity. The transformation applied depends on the current TransformationAxes.
        /// For all types of transformations, we calculate the change between the start click position and the current mouse position instead of working with delta changes.
        /// This ensures that when the user returns to start click position the transformation is as it was at the beginning of the transformation.
        /// The transformation direction is either horizontal (left->right) or vertical (bottom->top) and is determined at the beginning of the gesture depending on the user move direction.
        /// </summary>
        private async Task TransformSceneEntityBase()
        {
            if (!Input.IsKeyDown(Keys.LeftCtrl) && !Input.IsKeyDown(Keys.RightCtrl))
                duplicationDone = false;

            // skip the update if no transformation is currently performed
            if (!IsEnabled || AnchorEntity == null || TransformationAxes == GizmoTransformationAxes.None || !Input.IsMouseButtonDown(MouseButton.Left))
            {
                if (transformationInitialized)
                    OnTransformationFinished();

                transformationInitialized = false;
                transformationStarted = false;
                return;
            }

            // initialize the start values at the beginning of the transformation
            if (!transformationInitialized)
            {
                InitializeTransformation();
            }

            // calculate the current drag translation in the screen normalized space
            var mousePosition = Input.MousePosition;
            var mouseDrag = mousePosition - StartMousePosition;

            // start the transformation only if user has dragged from a given amount of pixel. Determine direction of the transformation.
            if (!transformationStarted)
            {
                // ensure that the mouse cursor has been moved enough
                var screenSize = new Vector2(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);
                var mouseDragPixel = mouseDrag * screenSize;
                if (mouseDragPixel.Length() < TransformationStartPixelThreshold)
                    return;

                TransformationDirection = Math.Abs(mouseDragPixel.X) > Math.Abs(mouseDragPixel.Y) ? Vector2.UnitX : Vector2.UnitY;

                // ensure that the current transformation is not the identity (due to snap it might require more mouse movement to actually start the transformation)
                var currentTransformation = CalculateTransformation();
                if (currentTransformation.IsIdentity())
                    return;

                // check if Ctrl is pressed and initiate a duplication in this case.
                if (ModifiedEntities.Count > 0 && !duplicationDone && Input.IsKeyDown(Keys.LeftCtrl) || Input.IsKeyDown(Keys.RightCtrl))
                {
                    duplicationDone = true;
                    await Game.EditorServices.Get<IEditorGameEntitySelectionService>().DuplicateSelection();
                }

                OnTransformationStarted(mouseDragPixel);
            }
            
            // determine the transformation to apply
            var transformation = CalculateTransformation();

            // apply the transformations on all selected root entities
            foreach (var entity in ModifiedEntities)
            {
                var initialTransfo = InitialTransformations[entity];
                var entityTransfo = entity.Transform;
                
                if (initialTransfo.InverseParentMatrix == Matrix.Zero)
                {
                    // This usually occurs when at least one axis is scaled to zero (because the matrix inversion
                    // function returns Matrix.Zero if the determinant is too small).

                    // TODO: I added this fix because I didn't know how else to make this case work.
                    // It might break in some cases, which I haven't come across yet.
                    // But at least it now lets you transform an object that has at least one axis scaled to zero.
                    // - Mirsad
                    // Note: -> This does not work in the case one of the parent entity have a rotation.
                    // To completely fix the problem of 0-scaled objects, gizmo motion projections should be calculated in the object space.
                    // But this required a deeper modification of the current source code (transformation projection per object, etc.)
                    // - Pierre
                    initialTransfo.InverseParentMatrix = Matrix.Identity;
                }

                // calculate the gizmo to parent space matrix
                Matrix gizmoToParentMatrix;
                Matrix.Multiply(ref StartWorldMatrix, ref initialTransfo.InverseParentMatrix, out gizmoToParentMatrix);
                
                // the scale
                entityTransfo.Scale = initialTransfo.Scale * transformation.Scale;

                // translation (transform the translation from gizmo space to the selected root's parent space)
                entityTransfo.Position = initialTransfo.Translation + Vector3.TransformNormal(transformation.Translation, gizmoToParentMatrix);

                // the rotation
                if (transformation.Rotation != Quaternion.Identity)
                {
                    var entityToGizmoMatrix = Matrix.Invert(gizmoToParentMatrix * Matrix.Translation(-initialTransfo.Translation));
                    var entityToRotatedGizmoMatrix = entityToGizmoMatrix * Matrix.RotationQuaternion(transformation.Rotation);
                    var elementTranslationGizmo = entityToRotatedGizmoMatrix.TranslationVector - entityToGizmoMatrix.TranslationVector;
                    entityTransfo.Position = initialTransfo.Translation + Vector3.TransformNormal(elementTranslationGizmo, gizmoToParentMatrix);

                    var rotationAxisParent = Vector3.Normalize(Vector3.TransformNormal(transformation.Rotation.Axis, gizmoToParentMatrix));
                    entityTransfo.Rotation = initialTransfo.Rotation * Quaternion.RotationAxis(rotationAxisParent, transformation.Rotation.Angle);
                }
            }
        }

        public virtual async Task Update()
        {
            if (!IsEnabled)
                return;

            UpdateShape();
            UpdateTransformationAxisBase();
            UpdateColors();
            await TransformSceneEntityBase();
            UpdateTransformation();
        }

        /// <summary>
        /// Update the shape of the gizmo depending on the angle of view.
        /// </summary>
        protected virtual void UpdateShape()
        {
        }

        public void ClearTransformationAxes()
        {
            if (!duplicationDone)
                TransformationAxes = GizmoTransformationAxes.None;
        }
    }
}
