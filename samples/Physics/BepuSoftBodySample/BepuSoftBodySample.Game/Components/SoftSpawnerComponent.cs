// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace BepuSoftBodySample.Game.Components;

/// <summary>
/// Drops copies of a prefab one after the other above this entity, each with one of the given materials.
/// </summary>
[ComponentCategory("BepuSoftBodyDemo")]
public class SoftSpawnerComponent : SyncScript
{
    private readonly Random _random = new(1);
    private float _time;
    private int _spawned;

    /// <summary> What to drop, typically an entity with a model and a soft body </summary>
    public Prefab? Prefab { get; set; }

    /// <summary> Picked in turn for the model of each copy </summary>
    public List<Material> Materials { get; } = new();

    /// <summary> How many copies to drop </summary>
    public int Count { get; set; } = 20;

    /// <summary> Seconds between two copies </summary>
    public float Interval { get; set; } = 0.15f;

    /// <summary> Copies appear anywhere in this box around the entity </summary>
    public Vector3 Spread { get; set; } = new(2f, 1f, 2f);

    /// <summary> Each copy is scaled by a random factor between one and this </summary>
    public float MaximumScale { get; set; } = 1.3f;

    public override void Update()
    {
        if (Prefab is null)
            return;

        _time += (float)Game.UpdateTime.Elapsed.TotalSeconds;
        while (_spawned < Count && _time >= _spawned * Interval)
        {
            var offset = new Vector3(_random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f, _random.NextSingle() - 0.5f) * Spread;
            var scale = MathUtil.Lerp(1f, MaximumScale, _random.NextSingle());
            foreach (var entity in Prefab.Instantiate())
            {
                entity.Transform.Position = Entity.Transform.WorldMatrix.TranslationVector + offset;
                entity.Transform.Scale *= scale;
                if (Materials.Count > 0 && entity.Get<ModelComponent>() is { } model)
                    model.Materials[0] = Materials[_spawned % Materials.Count];
                Entity.Scene.Entities.Add(entity);
            }
            _spawned++;
        }
    }
}
