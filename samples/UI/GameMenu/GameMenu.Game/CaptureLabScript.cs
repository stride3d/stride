// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace GameMenu
{
    /// <summary>
    /// Measures how UI hit-testing behaves in the situations that input capture could affect: a moving button, a world-space
    /// panel seen by an orbiting camera and drawn by a second camera, and window resizes.
    /// </summary>
    /// <remarks>
    /// Clicks that hit a target are counted on the target. Clicks the game sees are counted separately. With masking on,
    /// clicks on a target do not reach the game, so the game count is the number of clicks that missed every target.
    /// </remarks>
    public class CaptureLabScript : UISceneBase
    {
        /// <summary>
        /// The scene to return to.
        /// </summary>
        public UrlReference<Scene> MenuSceneUrl { get; set; }

        /// <summary>
        /// How far the moving target travels each second, as a fraction of the canvas width.
        /// </summary>
        public float MovingTargetSpeed { get; set; } = 0.35f;

        private Button movingTarget;
        private TextBlock statsText;
        private int movingTargetHits;
        private int worldTargetHits;
        private int gameClicks;
        private int framesSinceResize = -1;
        private float movingTargetPosition = 0.1f;
        private float movingTargetDirection = 1f;

        /// <summary>
        /// The entity with the world-space panel.
        /// </summary>
        public Entity WorldPanel { get; set; }

        protected override void LoadScene()
        {
            var root = Entity.Get<UIComponent>().Page.RootElement;

            movingTarget = root.FindVisualChildOfType<Button>("catchMeButton");
            statsText = root.FindVisualChildOfType<TextBlock>("statsText");
            var backButton = root.FindVisualChildOfType<Button>("backButton");

            var worldPanel = WorldPanel ?? Entity.Scene.Entities.FirstOrDefault(e => e.Name == "CaptureLabWorld");
            var worldTarget = worldPanel?.Get<UIComponent>()?.Page?.RootElement.FindVisualChildOfType<Button>("worldTargetButton");

            if (movingTarget == null || statsText == null || backButton == null || worldTarget == null)
            {
                Log.Error("Capture lab elements not found:" +
                    (movingTarget == null ? " catchMeButton" : "") +
                    (statsText == null ? " statsText" : "") +
                    (backButton == null ? " backButton" : "") +
                    (worldTarget == null ? " worldTargetButton (on the CaptureLabWorld entity)" : ""));
            }

            if (movingTarget != null)
                movingTarget.Click += (_, _) => movingTargetHits++;
            if (backButton != null)
                backButton.Click += (_, _) => ReturnToMenu();
            if (worldTarget != null)
                worldTarget.Click += (_, _) => worldTargetHits++;

            Game.Window.ClientSizeChanged += OnClientSizeChanged;
        }

        protected override void UpdateScene()
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left))
                gameClicks++;

            if (framesSinceResize >= 0)
                framesSinceResize++;

            if (movingTarget != null)
                MoveTarget((float)Game.UpdateTime.Elapsed.TotalSeconds);

            if (statsText == null)
                return;

            statsText.Text =
                $"Moving target hits: {movingTargetHits}\n" +
                $"World panel hits: {worldTargetHits}\n" +
                $"Clicks the game saw: {gameClicks}\n" +
                $"Masking: {(Input.MaskCapturedInput ? "on" : "off")} (F9)\n" +
                $"Frames since resize: {(framesSinceResize < 0 ? "-" : framesSinceResize.ToString())}";
        }

        public override void Cancel()
        {
            Game.Window.ClientSizeChanged -= OnClientSizeChanged;
            base.Cancel();
        }

        private void MoveTarget(float elapsedSeconds)
        {
            movingTargetPosition += movingTargetDirection * MovingTargetSpeed * elapsedSeconds;
            if (movingTargetPosition > 0.9f || movingTargetPosition < 0.1f)
            {
                movingTargetDirection = -movingTargetDirection;
                movingTargetPosition = Math.Clamp(movingTargetPosition, 0.1f, 0.9f);
            }

            var current = movingTarget.GetCanvasRelativePosition();
            movingTarget.SetCanvasRelativePosition(new Vector3(movingTargetPosition, current.Y, current.Z));
        }

        private void OnClientSizeChanged(object sender, EventArgs e) => framesSinceResize = 0;

        private void ReturnToMenu()
        {
            SceneSystem.SceneInstance.RootScene = Content.Load(MenuSceneUrl);
            Cancel();
        }
    }
}
