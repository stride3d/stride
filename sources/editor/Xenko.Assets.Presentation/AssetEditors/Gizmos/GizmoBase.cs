// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Editor.EditorGame.Game;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Input;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// The base class for scene editor's gizmos.
    /// </summary>
    public abstract class GizmoBase : ComponentBase, IGizmo
    {
        public static readonly PropertyKey<bool> NoGizmoKey = new PropertyKey<bool>("NoGizmo", typeof(GizmoBase));

        private bool isEnabled = true;
        private IGraphicsDeviceService graphicsDeviceService;

        private float sizeFactor;

        /// <summary>
        /// The default entity group of the gizmo.
        /// </summary>
        public const RenderGroup DefaultGroup = RenderGroup.Group0;
        
        /// <summary>
        /// The entity group of the physics gizmo.
        /// </summary>
        public const RenderGroup PhysicsShapesGroup = RenderGroup.Group7;

        /// <summary>
        /// The entity group of the particle gizmo.
        /// </summary>
        public const RenderGroup ParticlesShapesGroup = PhysicsShapesGroup; // Reuse the same wireframe render feature

        /// <summary>
        /// The entity group of the light shaft volume gizmo.
        /// </summary>
        public const RenderGroup LightShaftsGroup = PhysicsShapesGroup; // Reuse the same wireframe render feature

        /// <summary>
        /// The default entity group of the gizmo.
        /// </summary>
        public const RenderGroupMask DefaultGroupMask = RenderGroupMask.Group0;

        /// <summary>
        /// The entity group of the physics gizmo.
        /// </summary>
        public const RenderGroupMask PhysicsShapesGroupMask = RenderGroupMask.Group7;

        /// <summary>
        /// The entity group of the particles gizmo.
        /// </summary>
        public const RenderGroupMask ParticlesShapesGroupMask = PhysicsShapesGroupMask;

        /// <summary>
        /// Gets or sets the size scale factor of the gizmo.
        /// </summary>
        public virtual float SizeFactor { get { return sizeFactor; } set { sizeFactor = value; } }

        /// <summary>
        /// Gets the editor scene where the gizmo should be contained.
        /// </summary>
        protected Scene EditorScene { get; private set; }

        /// <summary>
        /// Gets a registry containing the services available for the gizmos.
        /// </summary>
        protected IServiceRegistry Services { get; private set; }

        /// <summary>
        /// Gets a reference to the input manager.
        /// </summary>
        protected InputManager Input { get; private set; }

        /// <summary>
        /// Gets a reference to the game.
        /// </summary>
        protected EditorServiceGame Game { get; private set; }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        protected GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// Gets the current command list.
        /// </summary>
        public CommandList GraphicsCommandList { get; private set; }

        /// <summary>
        /// Gets the factor used to adjust specifically the size of this gizmo
        /// </summary>
        protected virtual float SizeAdjustmentFactor => 1;

        /// <summary>
        /// Gets the root entity of the gizmo.
        /// </summary>
        public Entity GizmoRootEntity { get; private set; }

        /// <summary>
        /// Gets or sets the entity group of the Gizmo
        /// </summary>
        public RenderGroup RenderGroup { get; protected set; }
        
        /// <summary>
        /// Creates a new instance of <see cref="GizmoBase"/>.
        /// </summary>
        protected GizmoBase()
        {
            sizeFactor = 1;
        }

        public virtual bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                var hasChanged = value != isEnabled;
                isEnabled = value;

                if (GizmoRootEntity == null || !hasChanged)
                    return;

                if (value)
                    EditorScene.Entities.Add(GizmoRootEntity);
                else
                    EditorScene.Entities.Remove(GizmoRootEntity);
            }
        }

        public virtual bool IsUnderMouse(int pickedComponentId)
        {
            return false;
        }

        public virtual void Initialize(IServiceRegistry services, Scene editorScene)
        {
            Services = services;
            EditorScene = editorScene;
            Input = Services.GetService<InputManager>();
            Game = (EditorServiceGame)Services.GetSafeServiceAs<IGame>();
            graphicsDeviceService = Services.GetSafeServiceAs<IGraphicsDeviceService>();
            GraphicsDevice = graphicsDeviceService.GraphicsDevice;
            GraphicsCommandList = Game.GraphicsContext.CommandList;
            GizmoRootEntity = Create();
            if (GizmoRootEntity != null)
            {
                EditorScene.Entities.Add(GizmoRootEntity);
            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            if (GizmoRootEntity != null)
                EditorScene.Entities.Remove(GizmoRootEntity);
        }

        /// <summary>
        /// Create the gizmo entity hierarchy.
        /// </summary>
        /// <returns>the gizmo root entity</returns>
        protected abstract Entity Create();
    }
}
