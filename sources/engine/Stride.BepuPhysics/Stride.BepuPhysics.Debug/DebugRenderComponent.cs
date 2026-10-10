// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Input;

namespace Stride.BepuPhysics.Debug;

/// <summary>
/// Draws the colliders and the solver contacts of the Bepu simulations, toggled at runtime with <see cref="Key"/>.
/// </summary>
/// <remarks> Wireframes and contact lines are expanded by geometry shaders, so this needs <see cref="Stride.Graphics.GraphicsProfile.Level_10_0"/> or higher. </remarks>
[DataContract]
[DefaultEntityComponentProcessor(typeof(DebugRenderProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Bepu - Debug")]
public class DebugRenderComponent : SyncScript
{
    internal DebugRenderProcessor? _processor;
    bool _state = true;

    public Keys Key { get; set; } = Keys.F11;

    [DataMember]
    public bool Visible
    {
        get => _processor?.Visible ?? _state;
        set
        {
            _state = value;
            if (_processor is not null)
                _processor.Visible = value;
        }
    }

    /// <summary>
    /// Whether to draw the wireframe of each collider.
    /// </summary>
    [DataMember]
    public bool ShowShapes { get; set; } = true;

    /// <summary>
    /// Whether to draw the back faces of the colliders as dashed lines; like the other lines, other objects hide them, their own visual model does not.
    /// </summary>
    [DataMember]
    public bool ShowBackFaces { get; set; }

    /// <summary>
    /// Whether to draw a marker at each contact point the solver is currently resolving.
    /// </summary>
    [DataMember]
    public bool ShowContactPoints { get; set; }

    /// <summary>
    /// Whether to draw the normal of each contact the solver is currently resolving.
    /// </summary>
    [DataMember]
    public bool ShowContactNormals { get; set; }

    /// <summary>
    /// The length of the contact normals, in world units; the point markers are scaled from it.
    /// </summary>
    [DataMember]
    public float ContactSize { get; set; } = 0.25f;

    public override void Update()
    {
        if (Input.IsKeyPressed(Key))
        {
            Visible = !Visible;
        }
    }
}
