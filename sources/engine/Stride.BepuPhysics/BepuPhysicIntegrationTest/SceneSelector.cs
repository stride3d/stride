using BepuPhysicIntegrationTest.Integration;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest
{
    public class SceneSelector : SyncScript
    {
        public Scene? MainScene { get; set; }

        public UrlReference<Scene>? Scene0 { get; set; }
        public UrlReference<Scene>? Scene1 { get; set; }
        public UrlReference<Scene>? Scene2 { get; set; }
        public UrlReference<Scene>? Scene3 { get; set; }
        public UrlReference<Scene>? Scene4 { get; set; }
        public UrlReference<Scene>? Scene5 { get; set; }
        public UrlReference<Scene>? Scene6 { get; set; }
        public UrlReference<Scene>? Scene7 { get; set; }
        public UrlReference<Scene>? Scene8 { get; set; }
        public UrlReference<Scene>? Scene9 { get; set; }

        private Scene? _last { get; set; } = null;

        public override void Start()
        {
            Game.Window.AllowUserResizing = true;
            //Game.Window.IsFullscreen = true;
            Game.Window.Title = "StrideGo";
        }
        public override void Update()
        {
            DebugText.Print("USE NUMPAD number :", new(BepuAndStrideExtensions.X_DEBUG_TEXT_POS, 500));

            for (int i = 0; i < 10; i++)
            {
                var sceneRef = GetSceneRef(i);
                if (sceneRef == null)
                    continue;

                DebugText.Print($"{i} => {sceneRef.Url}", new(BepuAndStrideExtensions.X_DEBUG_TEXT_POS, 500 + ((i + 1) * 25)));

                if (Input.IsKeyPressed(Keys.NumPad0 + i) || Input.IsKeyPressed(Keys.D0 + i))
                {
                    SetScene(sceneRef);
                }
            }
        }

        private void SetScene(UrlReference<Scene> sceneRef)
        {
            if (MainScene == null)
                return;

            if (_last != null)
            {
                MainScene.Children.Clear();
                Content.Unload(_last);
                _last.Dispose();
            }

            if (sceneRef != null)
            {
                _last = Content.Load(sceneRef);
                MainScene.Children.Add(_last);
            }
        }
        private UrlReference<Scene>? GetSceneRef(int index)
        {
            switch (index)
            {
                case 0:
                    return Scene0;
                case 1:
                    return Scene1;
                case 2:
                    return Scene2;
                case 3:
                    return Scene3;
                case 4:
                    return Scene4;
                case 5:
                    return Scene5;
                case 6:
                    return Scene6;
                case 7:
                    return Scene7;
                case 8:
                    return Scene8;
                case 9:
                    return Scene9;
                default:
                    return null;
            }
        }

    }
}
