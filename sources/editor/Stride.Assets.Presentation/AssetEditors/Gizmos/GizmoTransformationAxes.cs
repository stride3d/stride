// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// Enumerates the different possible axes of the transformation for the gizmo.
    /// </summary>
    [Flags]
    public enum GizmoTransformationAxes
    {
        /// <summary>
        /// No transformation axes.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Ox axes.
        /// </summary>
        X = 1,

        /// <summary>
        /// The Oy axes.
        /// </summary>
        Y = 2,

        /// <summary>
        /// The Oz axes.
        /// </summary>
        Z = 4,

        /// <summary>
        /// The Oxy plane.
        /// </summary>
        XY = X|Y,

        /// <summary>
        /// The Oxz plane.
        /// </summary>
        XZ = X|Z,
        
        /// <summary>
        /// The Oyz plane.
        /// </summary>
        YZ = Y|Z,

        /// <summary>
        /// Any axes.
        /// </summary>
        XYZ = X|Y|Z,
    }
}
