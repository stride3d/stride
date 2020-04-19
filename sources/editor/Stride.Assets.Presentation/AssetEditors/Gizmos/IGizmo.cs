// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// The base interface for editor gizmos
    /// </summary>
    public interface IGizmo : IDisposable
    {
        /// <summary>
        /// Gets or sets the enabled state of the gizmo.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the size scale factor of the gizmo.
        /// </summary>
        float SizeFactor { get; set; }

        /// <summary>
        /// Indicate if the mouse is over the gizmo.
        /// </summary>
        /// <param name="pickedComponentId"></param>
        /// <returns><value>True</value> if the mouse is over the gizmo</returns>
        bool IsUnderMouse(int pickedComponentId);

        /// <summary>
        /// Initialize the gizmo.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="editorScene"></param>
        void Initialize(IServiceRegistry services, Scene editorScene);
    }
}
