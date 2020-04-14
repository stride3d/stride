// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using SpaceEscape.Background;

namespace SpaceEscape
{
    /// <summary>
    /// GameScript manages all entities in the game: Camera, CharacterScript, BackgroundScript and Obstacles.
    /// </summary>
    public class GameScript : SyncScript
    {
        /// <summary>
        /// The reference to the character script
        /// </summary>
        public CharacterScript CharacterScript;

        /// <summary>
        /// The reference to the background script
        /// </summary>
        public BackgroundScript BackgroundScript;

        /// <summary>
        /// The reference to the UI script
        /// </summary>
        public UIScript UIScript;

        public override void Start()
        {
            // Enable visual of mouse in the game
            Game.Window.IsMouseVisible = true;

            // Update the distance displayed in the UI
            BackgroundScript.DistanceUpdated += SetDistanceInUI;

            // set behavior of UI button
            UIScript.StartButton.Click += StartGame;
            UIScript.RetryButton.Click += RestartGame;
            UIScript.MenuButton.Click += GoToMenu;
            
            GoToMenu(this, EventArgs.Empty);
        }

        /// <summary>
        /// Script update loop that detect collision between CharacterScript an obstacles, 
        /// and detect if the CharacterScript falls to any hole.
        /// </summary>
        /// <returns></returns>
        public override void Update()
        {
            if (CharacterScript.IsDead)
                return;

            float floorHeight;
            var agentBoundingBox = CharacterScript.CalculateCurrentBoundingBox();

            // Detect collision between agents and real-world obstacles.
            if (BackgroundScript.DetectCollisions(ref agentBoundingBox))
                KillAgent(0);

            // Detect if the CharacterScript falls into a hole
            if (BackgroundScript.DetectHoles(ref CharacterScript.Entity.Transform.Position, out floorHeight))
                KillAgent(floorHeight);
        }

        public override void Cancel()
        {
            BackgroundScript.DistanceUpdated -= SetDistanceInUI;

            UIScript.StartButton.Click -= StartGame;
            UIScript.RetryButton.Click -= RestartGame;
            UIScript.MenuButton.Click -= GoToMenu;
        }

        private void SetDistanceInUI(float curDist)
        {
            UIScript.SetDistance((int)curDist);
        }

        /// <summary>
        /// Kills the player.
        /// </summary>
        private void KillAgent(float height)
        {
            CharacterScript.OnDied(height);
            UIScript.StartGameOverMode();
            BackgroundScript.StopScrolling();
        }

        /// <summary>
        /// Reset game's entities: CharacterScript and LevelBlocks.
        /// </summary>
        private void ResetGame()
        {
            CharacterScript.Reset();
            BackgroundScript.Reset();
        }

        /// <summary>
        /// Restart playing
        /// </summary>
        private void RestartGame(object sender, EventArgs args)
        {
            ResetGame();
            StartGame(sender, args);
        }

        /// <summary>
        /// Start playing
        /// </summary>
        private void StartGame(object sender, EventArgs args)
        {
            UIScript.StartPlayMode();
            BackgroundScript.StartScrolling();
            CharacterScript.Activate();
        }

        /// <summary>
        /// Go to the menu screen
        /// </summary>
        private void GoToMenu(object sender, EventArgs args)
        {
            UIScript.StartMainMenuMode();
            ResetGame();
        }
    }
}
