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

    public override void Update()
    {
        if (Input.IsKeyPressed(Key))
        {
            Visible = !Visible;
        }
    }
}
