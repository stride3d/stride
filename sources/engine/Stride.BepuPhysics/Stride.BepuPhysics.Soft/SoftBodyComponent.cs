// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Soft.Definitions;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.BepuPhysics.Soft
{
    [ComponentCategory("Bepu")]
    public class SoftBodyComponent : StartupScript
    {
        private int _simulationIndex = 0;
        private Model _model;
        private List<BodyHandle> _bodies;

        [DataMemberIgnore]
        public BepuSimulation? Simulation { get; private set; }

        public int SimulationIndex
        {
            get
            {
                return _simulationIndex;
            }
            set
            {
                _simulationIndex = value;
            }
        }

        [MemberRequired(ReportAs = MemberRequiredReportType.Error)]
        public required Model Model
        {
            get => _model;
            set
            {
                _model = value;
            }
        }

        public override void Start()
        {
            base.Start();
            Simulation = Services.GetService<BepuConfiguration>().BepuSimulations[_simulationIndex];
            var modelData = ShapeCacheSystem.ExtractBepuMesh(Model, Game.Services, Simulation.BufferPool);
            Newt.Create(Simulation, modelData, Entity.Transform.WorldMatrix.TranslationVector.ToNumeric(), out var bodies);
            _bodies = bodies;
        }

        public override void Cancel()
        {
            foreach (var item in _bodies)
            {
                Simulation!.Simulation.Bodies.Remove(item);
            }
            base.Cancel();
        }

    }
}
