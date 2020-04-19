// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Physics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using System.Threading.Tasks;

namespace PhysicsSample12
{
    public class DemoScript : StartupScript
    {
        private Simulation simulation;

        public Entity cube;
        public Entity sphere;

        public SpriteFont Font;

        private Constraint currentConstraint;
        private readonly List<Action> PhysicsSampleList = new List<Action>();
        private int constraintIndex;

        private RigidbodyComponent cubeRigidBody;
        private RigidbodyComponent sphereRigidBody;

        private TextBlock constraintNameBlock;

        public override void Start()
        {
            simulation = this.GetSimulation();
            simulation.Gravity = new Vector3(0, -9, 0);

            cubeRigidBody = cube.Get<RigidbodyComponent>();
            cubeRigidBody.CanSleep = false;
            sphereRigidBody = sphere.Get<RigidbodyComponent>();
            sphereRigidBody.CanSleep = false;

            // Create the UI
            constraintNameBlock = new TextBlock
            {
                Font = Font,
                TextSize = 55,
                TextColor = Color.White,
            };
            constraintNameBlock.SetCanvasPinOrigin(new Vector3(0.5f, 0.5f, 0));
            constraintNameBlock.SetCanvasRelativePosition(new Vector3(0.5f, 0.83f, 0));

            Entity.Get<UIComponent>().Page = new UIPage
            {
                RootElement = new Canvas
                {
                    Children =
                    {
                        constraintNameBlock,
                        CreateButton("Next", Font, 1),
                        CreateButton("Previous", Font, -1)
                    }
                }
            };

            // Create and initialize constraint
            PhysicsSampleList.Add(CreatePoint2PointConstraint);
            PhysicsSampleList.Add(CreateHingeConstraint);
            PhysicsSampleList.Add(CreateGearConstraint);
            PhysicsSampleList.Add(CreateSliderConstraint);
            PhysicsSampleList.Add(CreateConeTwistConstraint);
            PhysicsSampleList.Add(CreateGeneric6DoFConstraint);

            RemoveConstraint();
            PhysicsSampleList[constraintIndex]();

            //Add a script for the slider constraint, to apply an impulse on collision
            cubeRigidBody.ProcessCollisions = true;
            Script.AddTask(async () =>
            {
                while (Game.IsRunning)
                {
                    var collision = await cubeRigidBody.NewCollision();
                    if (!(currentConstraint is SliderConstraint)) continue;
                    if (collision.ColliderA != sphereRigidBody && collision.ColliderB != sphereRigidBody) continue;
                    sphereRigidBody.LinearVelocity = Vector3.Zero; //clear any existing velocity
                    sphereRigidBody.ApplyImpulse(new Vector3(-25, 0, 0)); //fire impulse
                }
            });
        }

        void CreatePoint2PointConstraint()
        {
            cubeRigidBody.LinearFactor = Vector3.Zero;
            cubeRigidBody.AngularFactor = Vector3.Zero;
            sphereRigidBody.LinearFactor = new Vector3(1, 1, 1);
            sphereRigidBody.AngularFactor = new Vector3(1, 1, 1);

            currentConstraint = Simulation.CreateConstraint(ConstraintTypes.Point2Point, cubeRigidBody, sphereRigidBody,
                Matrix.Identity, Matrix.Translation(new Vector3(4, 0, 0)));
            simulation.AddConstraint(currentConstraint);
            constraintNameBlock.Text = "Point to Point";

            //there are no limits so the sphere will orbit once we apply this
            sphereRigidBody.ApplyImpulse(new Vector3(0, 0, 18));
        }

        void CreateHingeConstraint()
        {
            cubeRigidBody.LinearFactor = Vector3.Zero;
            cubeRigidBody.AngularFactor = Vector3.Zero;
            sphereRigidBody.LinearFactor = new Vector3(1, 1, 1);
            sphereRigidBody.AngularFactor = new Vector3(1, 1, 1);

            currentConstraint = Simulation.CreateConstraint(ConstraintTypes.Hinge, cubeRigidBody, sphereRigidBody,
                Matrix.Identity, Matrix.Translation(new Vector3(4, 0, 0)));
            simulation.AddConstraint(currentConstraint);
            constraintNameBlock.Text = "Hinge";

            //applying this impulse will show the hinge limits stopping it
            sphereRigidBody.ApplyImpulse(new Vector3(0, 0, 18));
        }

        void CreateGearConstraint()
        {
            cubeRigidBody.LinearFactor = Vector3.Zero;
            cubeRigidBody.AngularFactor = new Vector3(1, 1, 1);
            sphereRigidBody.LinearFactor = Vector3.Zero;
            sphereRigidBody.AngularFactor = new Vector3(1, 1, 1);

            currentConstraint = Simulation.CreateConstraint(ConstraintTypes.Gear, sphereRigidBody, cubeRigidBody,
                Matrix.Translation(new Vector3(1, 0, 0)), Matrix.Translation(new Vector3(1, 0, 0)));
            simulation.AddConstraint(currentConstraint);
            constraintNameBlock.Text = "Gear";

            var gear = (GearConstraint)currentConstraint;
            gear.Ratio = 0.5f;

            //this force will start a motion in the sphere which gets propagated into the cube
            sphereRigidBody.AngularVelocity = new Vector3(25, 0, 0);
        }

        void CreateSliderConstraint()
        {
            cubeRigidBody.LinearFactor = Vector3.Zero;
            cubeRigidBody.AngularFactor = Vector3.Zero;
            sphereRigidBody.LinearFactor = new Vector3(1, 1, 1);
            sphereRigidBody.AngularFactor = new Vector3(1, 1, 1);

            currentConstraint = Simulation.CreateConstraint(ConstraintTypes.Slider, cubeRigidBody, sphereRigidBody, Matrix.Identity, Matrix.Identity, true);
            simulation.AddConstraint(currentConstraint);
            constraintNameBlock.Text = "Slider";

            var slider = (SliderConstraint)currentConstraint;
            slider.LowerLinearLimit = -4;
            slider.UpperLinearLimit = 0;
            //avoid strange movements
            slider.LowerAngularLimit = (float)-Math.PI / 3.0f;
            slider.UpperAngularLimit = (float)Math.PI / 3.0f;

            //applying this impulse will let the sphere reach the lower linear limit and afterwards will be dragged back towards the cube
            sphereRigidBody.ApplyImpulse(new Vector3(-25, 0, 0));
        }

        void CreateConeTwistConstraint()
        {
            cubeRigidBody.LinearFactor = Vector3.Zero;
            cubeRigidBody.AngularFactor = Vector3.Zero;
            sphereRigidBody.LinearFactor = new Vector3(1, 1, 1);
            sphereRigidBody.AngularFactor = new Vector3(1, 1, 1);

            currentConstraint = Simulation.CreateConstraint(ConstraintTypes.ConeTwist, cubeRigidBody, sphereRigidBody,
                Matrix.Identity, Matrix.Translation(new Vector3(4, 0, 0)));
            simulation.AddConstraint(currentConstraint);
            constraintNameBlock.Text = "Cone Twist";

            var coneTwist = (ConeTwistConstraint)currentConstraint;
            coneTwist.SetLimit(0.5f, 0.5f, 0.5f);

            //applying this impulse will show the cone limits
            sphereRigidBody.ApplyImpulse(new Vector3(0, 0, 18));
        }

        void CreateGeneric6DoFConstraint()
        {
            cubeRigidBody.LinearFactor = Vector3.Zero;
            cubeRigidBody.AngularFactor = Vector3.Zero;
            sphereRigidBody.LinearFactor = new Vector3(1, 1, 1);
            sphereRigidBody.AngularFactor = new Vector3(1, 1, 1);

            currentConstraint = Simulation.CreateConstraint(ConstraintTypes.Generic6DoF, cubeRigidBody, sphereRigidBody,
                Matrix.Identity, Matrix.Translation(new Vector3(4, 0, 0)));
            simulation.AddConstraint(currentConstraint);
            constraintNameBlock.Text = "Generic 6D of Freedom";

            sphereRigidBody.ApplyImpulse(new Vector3(0, 0, 18));
        }

        private void RemoveConstraint()
        {
            //Remove and dispose the current constraint
            if (currentConstraint != null)
            {
                simulation.RemoveConstraint(currentConstraint);
                currentConstraint.Dispose();
				currentConstraint = null;
            }

            //Stop motion and reset the rigid bodies
            cubeRigidBody.PhysicsWorldTransform = Matrix.Translation(new Vector3(2, 0, -9)) *
                                                  Matrix.RotationQuaternion(new Quaternion(0, 0, 0, 1));

            cubeRigidBody.AngularVelocity = Vector3.Zero;
            cubeRigidBody.LinearVelocity = Vector3.Zero;

            sphereRigidBody.PhysicsWorldTransform = Matrix.Translation(new Vector3(-2, 0, -9)) *
                                                    Matrix.RotationQuaternion(new Quaternion(0, 0, 0, 1));

            sphereRigidBody.AngularVelocity = Vector3.Zero;
            sphereRigidBody.LinearVelocity = Vector3.Zero;
        }

        private void ChangeConstraint(int offset)
        {
            RemoveConstraint();

            // calculate constraint index
            constraintIndex = (constraintIndex + offset + PhysicsSampleList.Count) % PhysicsSampleList.Count;

            PhysicsSampleList[constraintIndex]();
        }

        private Button CreateButton(string text, SpriteFont font, int offset)
        {
            var button = new Button
            {
                Name = text,
                Padding = Thickness.UniformRectangle(15),
                HorizontalAlignment = HorizontalAlignment.Left,
                Content = new TextBlock { Text = text, Font = font, TextSize = 35, TextColor = new Color(200, 200, 200, 255) },
                BackgroundColor = new Color(new Vector4(0.2f, 0.2f, 0.2f, 0.2f)),
            };

            button.Click += (sender, args) =>
            {
                Script.AddTask(() =>
                {
                    ChangeConstraint(offset);
                    return Task.CompletedTask;
                });
            };

            button.SetCanvasPinOrigin(new Vector3(offset > 0 ? 1 : 0, 0.5f, 0));
            button.SetCanvasRelativePosition(new Vector3(offset > 0 ? 0.87f : 0.13f, 0.93f, 0));

            return button;
        }

        public override void Cancel()
        {
            RemoveConstraint();
        }
    }
}
