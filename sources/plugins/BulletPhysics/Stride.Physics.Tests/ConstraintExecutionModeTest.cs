// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Physics.Constraints;
using Stride.Physics.Engine;
using Xunit;

namespace Stride.Physics.Tests
{
    /// <summary>
    /// A constraint is created only where the simulation runs: not in a scene of the editor, a thumbnail or a preview, even
    /// when nothing switched the simulation off (<see cref="Simulation.DisableSimulation"/>).
    /// </summary>
    public class ConstraintExecutionModeTest
    {
        [Theory]
        [InlineData(ExecutionMode.Editor)]
        [InlineData(ExecutionMode.Thumbnail)]
        [InlineData(ExecutionMode.Preview)]
        public void AConstraintIsNotCreatedInAScene(ExecutionMode executionMode)
        {
            Assert.False(Simulation.DisableSimulation);
            var (scene, constraint) = CreateScene(executionMode);

            // The constraint component brings its processor in a running game only
            Assert.Null(scene.GetProcessor<PhysicsConstraintProcessor>());

            // A processor added by hand doesn't create it either
            scene.Processors.Add(new PhysicsConstraintProcessor());
            scene.Update(new GameTime());

            Assert.Null(constraint.Constraint);
        }

        [Fact]
        public void AConstraintIsCreatedInARunningGame()
        {
            Assert.False(Simulation.DisableSimulation);
            var (scene, _) = CreateScene(ExecutionMode.Runtime);
            Assert.NotNull(scene.GetProcessor<PhysicsConstraintProcessor>());

            // Its bodies have no simulation here: the processor tries to create the constraint, and says they aren't ready
            Assert.Throws<InvalidOperationException>(() => scene.Update(new GameTime()));
        }

        // A constraint between two bodies that aren't in the scene, so that no physics processor nor native body is created
        private static (SceneInstance Scene, PhysicsConstraintComponent Constraint) CreateScene(ExecutionMode executionMode)
        {
            var constraint = new PhysicsConstraintComponent
            {
                Description = new Point2PointConstraintDesc(),
                BodyA = new RigidbodyComponent(),
            };
            var scene = new SceneInstance(new ServiceRegistry(), new Scene { Entities = { new Entity { constraint } } }, executionMode);
            return (scene, constraint);
        }
    }
}
