// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;

namespace Stride.Engine.Gizmos
{
    /// <summary>
    /// An interface creating and managing an editor gizmo for a particular <see cref="EntityComponent"/>
    /// </summary>
    /// <remarks>
    /// The implementer must have a <see cref="GizmoComponentAttribute"/> attach and the constructor of said class 
    /// must have exactly one argument of the type of component specified in the attribute.
    /// </remarks>
    public interface IEntityGizmo : IGizmo
    {
        /// <summary>
        /// Gets or sets the selected state of the gizmo.
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// Updates the gizmo state.
        /// </summary>
        void Update();

        /// <summary>
        /// Render group used for scene picking, set your model to use this render group when you want mouse selection to work on your models while still being depth tested
        /// </summary>
        /// <remarks>
        /// Your <see cref="IGizmo.HandlesComponentId"/> takes care of confirming to the engine that the picked component is yours
        /// </remarks>
        public const RenderGroup PickingRenderGroup = RenderGroup.Group0;


        /// <summary>
        /// Render group used for scene picking, set your model to use this render group when you want mouse selection to work on your models while being drawn above other objects
        /// </summary>
        /// <remarks>
        /// Your <see cref="IGizmo.HandlesComponentId"/> takes care of confirming to the engine that the picked component is yours
        /// </remarks>
        public const RenderGroup PickingForegroundRenderGroup = RenderGroup.Group4;
    }
}
