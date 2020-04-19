// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// Specifies for which component the associated gizmo class is.
    /// </summary>
    public class GizmoComponentAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GizmoComponentAttribute"/> class.
        /// </summary>
        /// <param name="componentType">The type of component the related gizmo class is associated with.</param>
        /// <param name="isMainGizmo">Indicates whether this gizmo is a main gizmo.</param>
        public GizmoComponentAttribute(Type componentType, bool isMainGizmo)
        {
            ComponentType = componentType;
            IsMainGizmo = isMainGizmo;
            if (!typeof(EntityComponent).IsAssignableFrom(componentType))
                throw new ArgumentException(@"The type must be an EntityComponent type.", nameof(componentType));
        }

        /// <summary>
        /// Gets or sets the type of component the related gizmo class is associated with.
        /// </summary>
        public Type ComponentType { get; set; }

        /// <summary>
        /// Gets or sets whether this gizmo is a main gizmo. An entity will display only one of its main gizmo, corresponding to the component
        /// that has the highest priority set in its <see cref="DisplayAttribute"/>.
        /// </summary>
        public bool IsMainGizmo { get; set; }
    }
}
