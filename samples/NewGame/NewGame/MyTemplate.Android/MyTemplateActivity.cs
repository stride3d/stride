using Android.App;
using Android.OS;
using Android.Content.PM;

using Stride.Engine;
using Stride.Starter;

namespace MyTemplate.Android;

[Activity(MainLauncher = true,
          Label = "MyTemplate",
          ScreenOrientation = ScreenOrientation.Landscape,
          Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
          ConfigurationChanges = ConfigChanges.UiMode | ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
public class MyTemplateActivity : StrideActivity
{
    protected Game Game;

    protected override void OnRun()
    {
        base.OnRun();

        Game = new Game();
        Game.Run(GameContext);
    }

    protected override void OnDestroy()
    {
        Game.Dispose();

        base.OnDestroy();
    }
}
