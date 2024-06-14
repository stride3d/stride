// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Debug;

[DataContract]
[ComponentCategory("Bepu - Debug")]
public class DebugRenderComponent : SyncScript
{
    DebugRenderProcessor? _processor;
    bool _state = true;

    public Keys Key { get; set; } = Keys.F11;

    [DataMember]
    public bool Render
    {
        get => _processor?.Enabled ?? _state;
        set
        {
            _state = value;
            if (_processor is not null)
                _processor.Enabled = value;
        }
    }

    public override void Start()
    {
        base.Start();
        if (Services.GetService<DebugRenderProcessor>() is { } processor)
        {
            _processor = processor;
        }
        else
        {
            _processor = new DebugRenderProcessor(Services);
            Services.AddService(_processor);
        }

        _processor.Enabled = _state;
    }

    public override void Update()
    {
        if (Input.IsKeyPressed(Key))
        {
            Render = !Render;
        }
    }
}
