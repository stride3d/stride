// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Input;

namespace Stride.BepuPhysics.Debug;

[DataContract]
[DefaultEntityComponentProcessor(typeof(DebugRenderProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Bepu - Debug")]
public class DebugRenderComponent : SyncScript
{
    internal DebugRenderProcessor? _processor;
    bool _state = true;
    bool _showShapes = true;
    bool _showContactPoints;
    bool _showContactNormals;
    float _contactSize = 0.25f;

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
    public bool ShowShapes
    {
        get => _processor?.ShowShapes ?? _showShapes;
        set
        {
            _showShapes = value;
            if (_processor is not null)
                _processor.ShowShapes = value;
        }
    }

    /// <summary>
    /// Whether to draw a marker at each contact point the solver is currently resolving.
    /// </summary>
    [DataMember]
    public bool ShowContactPoints
    {
        get => _processor?.ShowContactPoints ?? _showContactPoints;
        set
        {
            _showContactPoints = value;
            if (_processor is not null)
                _processor.ShowContactPoints = value;
        }
    }

    /// <summary>
    /// Whether to draw the normal of each contact the solver is currently resolving.
    /// </summary>
    [DataMember]
    public bool ShowContactNormals
    {
        get => _processor?.ShowContactNormals ?? _showContactNormals;
        set
        {
            _showContactNormals = value;
            if (_processor is not null)
                _processor.ShowContactNormals = value;
        }
    }

    /// <summary>
    /// The length of the contact normals, in world units; the point markers are scaled from it.
    /// </summary>
    [DataMember]
    public float ContactSize
    {
        get => _processor?.ContactSize ?? _contactSize;
        set
        {
            _contactSize = value;
            if (_processor is not null)
                _processor.ContactSize = value;
        }
    }

    public override void Update()
    {
        if (Input.IsKeyPressed(Key))
        {
            Visible = !Visible;
        }
    }
}
