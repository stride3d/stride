// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;

namespace StrideAssetPlugin;

// A public script type shipped by this asset plugin. A consumer's scene attaches it to an entity;
// the asset compiler must load this assembly (declared in the packed sdpkg) to resolve the type
// during scene compilation.
public class SpinScript : SyncScript
{
    public float Speed { get; set; } = 1.0f;

    public override void Update()
    {
        Entity.Transform.Rotation *= Quaternion.RotationY(Speed * 0.01f);
    }
}
