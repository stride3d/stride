// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

public abstract class TwoBodyConstraintComponent<T> : ConstraintComponent<T>, ITwoBody where T : unmanaged, IConstraintDescription<T>, ITwoBodyConstraintDescription<T>
{
    public BodyComponent? A
    {
        get => this[0];
        set => this[0] = value;
    }

    public BodyComponent? B
    {
        get => this[1];
        set => this[1] = value;
    }

    public TwoBodyConstraintComponent() : base(2) { }
}
