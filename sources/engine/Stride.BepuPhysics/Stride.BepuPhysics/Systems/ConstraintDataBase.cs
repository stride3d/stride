// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.BepuPhysics.Systems;

internal abstract class ConstraintDataBase
{
    protected static Logger Logger = GlobalLogger.GetLogger(nameof(ConstraintDataBase));

    public abstract bool Exist { get; }

    internal abstract void RebuildConstraint();
    internal abstract void DestroyConstraint();
    internal abstract void TryUpdateDescription();
}
