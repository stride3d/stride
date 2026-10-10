using Stride.Core.Mathematics;
using Stride.Engine;

namespace MyTemplate;

/// <summary>
/// Spins its entity. The speed comes from the component or, when set, from a <see cref="MyTemplateData"/> content
/// compiled from a MyTemplate asset by the Assets companion.
/// </summary>
public class MyTemplateComponent : SyncScript
{
    public float Speed { get; set; } = 1.0f;

    public MyTemplateData? Data { get; set; }

    public override void Update()
    {
        var speed = Data?.Speed ?? Speed;
        Entity.Transform.Rotation *= Quaternion.RotationY(speed * (float)Game.UpdateTime.Elapsed.TotalSeconds);
    }
}
