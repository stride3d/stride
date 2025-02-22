// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.FlexibleProcessing;
using Stride.Games;
using Stride.Input;


namespace Stride.BepuPhysics.Demo.Components.Character;
[ComponentCategory("BepuDemo - Character")]
public class CharacterControllerComponent : CharacterComponent, IComponent<CharacterControllerComponent.UpdateCaller, CharacterControllerComponent>
{
    public Entity? CameraPivot { get; set; }
    public float MinCameraAngle { get; set; } = -90;
    public float MaxCameraAngle { get; set; } = 90;

    private Vector3 _cameraAngle;
    private UpdateCaller _processor = null!;

    private void Update()
    {
        if (_processor.Inputs.IsKeyPressed(Keys.Tab))
        {
            _processor.Game.IsMouseVisible = !_processor.Game.IsMouseVisible;
            if (_processor.Game.IsMouseVisible)
                _processor.Inputs.UnlockMousePosition();
            else
                _processor.Inputs.LockMousePosition(true);
        }

        Move();
        Rotate();
        if (_processor.Inputs.IsKeyPressed(Keys.Space))
            TryJump();
    }

    public override void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        Orientation = Quaternion.RotationY(_cameraAngle.Y); // Do it before physics tick to ensure it is interpolated properly
        base.SimulationUpdate(simulation, simTimeStep);
    }

    private void Move()
    {
        // Keyboard movement
        var moveDirection = Vector2.Zero;
        if (_processor.Inputs.IsKeyDown(Keys.W) || _processor.Inputs.IsKeyDown(Keys.Z))
            moveDirection.Y += 1;
        if (_processor.Inputs.IsKeyDown(Keys.S))
            moveDirection.Y -= 1;
        if (_processor.Inputs.IsKeyDown(Keys.A) || _processor.Inputs.IsKeyDown(Keys.Q))
            moveDirection.X -= 1;
        if (_processor.Inputs.IsKeyDown(Keys.D))
            moveDirection.X += 1;

        var velocity = new Vector3(moveDirection.X, 0, -moveDirection.Y);
        velocity.Normalize();

        velocity = Vector3.Transform(velocity, Entity.Transform.Rotation);

        if (_processor.Inputs.IsKeyDown(Keys.LeftShift))
            velocity *= 2f;

        Move(velocity);
    }

    private void Rotate()
    {
        var delta = _processor.Inputs.Mouse.Delta;

        _cameraAngle.X -= delta.Y;
        _cameraAngle.Y -= delta.X;
        _cameraAngle.X = MathUtil.Clamp(_cameraAngle.X, MinCameraAngle, MaxCameraAngle);

        Entity.Transform.Rotation = Quaternion.RotationY(_cameraAngle.Y);
        if (CameraPivot != null)
        {
            CameraPivot.Transform.Rotation = Quaternion.RotationX(_cameraAngle.X);
        }
    }

    private class UpdateCaller : IComponent<UpdateCaller,CharacterControllerComponent>.IProcessor, IUpdateProcessor
    {
        private List<CharacterControllerComponent> Components = new();

        public InputManager Inputs;
        public IGame Game;

        public int Order { get; }

        public void SystemAdded(IServiceRegistry registryParam)
        {
            Inputs = registryParam.GetService<InputManager>();
            Game = registryParam.GetService<IGame>();
            Inputs.LockMousePosition(true);
            Game.IsMouseVisible = false;
        }
        public void SystemRemoved() { }

        public void OnComponentAdded(CharacterControllerComponent item)
        {
            Components.Add(item);
            item._processor = this;
        }

        public void OnComponentRemoved(CharacterControllerComponent item)
        {
            Components.Remove(item);
            item._processor = null!;
        }

        public void Update(GameTime gameTime)
        {
            foreach (var comp in Components)
                comp.Update();
        }
    }
}
