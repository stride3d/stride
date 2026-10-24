// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.BuildEngine;

namespace Stride.Editor.Services.Tests.Helpers;

/// <summary>
/// A build unit with no build step. Its preparation can wait on a gate, which holds the builder's queue: the builder prepares
/// the units one at a time, in priority order, so the units pushed behind the gate are prepared in that order once it opens.
/// </summary>
public sealed class GatedBuildUnit : AssetBuildUnit
{
    private static readonly Guid ContextId = Guid.NewGuid();
    private readonly ManualResetEventSlim? gate;
    private readonly List<string> order;

    public GatedBuildUnit(string name, List<string> order, int priorityMajor, int priorityMinor = 0, ManualResetEventSlim? gate = null)
        : base(new AssetBuildUnitIdentifier(ContextId, AssetId.New()))
    {
        Name = name;
        this.order = order;
        this.gate = gate;
        PriorityMajor = priorityMajor;
        PriorityMinor = priorityMinor;
    }

    public string Name { get; }

    /// <summary>
    /// Set once the builder started preparing this unit.
    /// </summary>
    public ManualResetEventSlim Preparing { get; } = new();

    protected override ListBuildStep Prepare()
    {
        Preparing.Set();
        gate?.Wait(TimeSpan.FromSeconds(30));
        lock (order)
            order.Add(Name);
        return new ListBuildStep();
    }
}
