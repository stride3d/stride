using System;
using System.Linq;
using System.Security.Policy;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    public class SceneSelector : SyncScript
    {
        public Scene Me { get; set; }
        public UrlReference<Scene> Scene0 { get; set; }
        public UrlReference<Scene> Scene1 { get; set; }

        private Scene _last { get; set; } = null;

        public override void Start()
        {
            Game.Window.AllowUserResizing = true;
            //Game.Window.IsFullscreen = true;
            Game.Window.Title = "Bepu Physics V2";

            ((Stride.Games.GameBase)Game).MinimizedMinimumUpdateRate.MinimumElapsedTime = new TimeSpan(100000);
            ((Stride.Games.GameBase)Game).WindowMinimumUpdateRate.MinimumElapsedTime = new TimeSpan(100000);
            base.Start();
        }

        public override void Update()
        {
            DebugText.Print("USE NUMPAD number :", new(Extensions.X_TEXT_POS, 500));
            DebugText.Print("0 => blockchain", new(Extensions.X_TEXT_POS, 525));
            DebugText.Print("1 => Cube fontain", new(Extensions.X_TEXT_POS, 550));

            if (Input.IsKeyPressed(Keys.NumPad0))
            {
                if (_last != null)
                {
                    Me.Children.Clear();
                    Content.Unload(_last);
                    _last.Dispose();
                }
                _last = Content.Load(Scene0);
                Me.Children.Add(_last);
            }
            if (Input.IsKeyPressed(Keys.NumPad1))
            {
                if (_last != null)
                {
                    Me.Children.Clear();
                    Content.Unload(_last);
                    _last.Dispose();
                }
                _last = Content.Load(Scene1);
                Me.Children.Add(_last);
            }
        }
    }
}
