// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using DotRecast.Core;
using DotRecast.Detour.Dynamic.Colliders;
using DotRecast.Recast;
using Stride.Core;
using Stride.Engine;

namespace Stride.DotRecast.Definitions;

[DataContract(Inherited = true)]
public abstract class BaseNavigationCollider : IDtCollider
{
    protected int area;
    protected float flagMergeThreshold;

    public abstract float[] Bounds();

    public abstract void Rasterize(RcHeightfield hf, RcContext context);

    public abstract void Initialize(Entity entity, IServiceRegistry registry);
}
