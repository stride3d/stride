using Stride.BepuPhysics.Navigation.Definitions;
using Stride.BepuPhysics.Navigation.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.BepuPhysics.Navigation.Components;
[DataContract(nameof(RecastPhysicsNavigationComponent))]
[ComponentCategory("Bepu - Navigation")]
[DefaultEntityComponentProcessor(typeof(RecastPhysicsNavigationProcessor), ExecutionMode = ExecutionMode.Runtime)]
public class RecastPhysicsNavigationComponent : EntityComponent
{
    public float Speed { get; set; } = 5.0f;

    public CharacterComponent? PhysicsComponent { get; set; }

    /// <summary>
    /// True if a new path needs to be calculated, can be manually changed to force a new path to be calculated.
    /// </summary>
    [DataMemberIgnore]
    public bool ShouldMove
    {
        get => _shouldMove;
        set
        {
            _shouldMove = value;
            if(!value)
            {
                PhysicsComponent?.Move(Vector3.Zero);
            }
        }
    }

    private bool _shouldMove;

    [DataMemberIgnore]
    public bool SetNewPath { get; set; } = true;

    [DataMemberIgnore]
    public bool InSetPathQueue { get; set; }

    /// <summary>
    /// The target position for the agent to move to. will trigger IsDirty to be set to true.
    /// </summary>
    [DataMemberIgnore]
    public Vector3 Target;

    [DataMemberIgnore]
    public List<Vector3> Path = new();

    [DataMemberIgnore]
    public List<long> Polys = new();
}
