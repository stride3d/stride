// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Stride.Core;

namespace Stride.Engine.Gizmos
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
        /// Used for picking gizmos on mouse down, returns true when that component was created by this gizmo
        /// </summary>
        bool HandlesComponentId(OpaqueComponentId pickedComponentId, [MaybeNullWhen(false)] out Entity selection);

        /// <summary>
        /// Initialize the gizmo.
        /// </summary>
        void Initialize(IServiceRegistry services, Scene editorScene);
    }
}
