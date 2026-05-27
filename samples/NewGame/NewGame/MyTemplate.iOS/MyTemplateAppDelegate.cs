using Foundation;
using UIKit;
using Stride.Engine;
using Stride.Starter;

namespace MyTemplate.iOS;

[Register("MyTemplateAppDelegate")]
public class MyTemplateAppDelegate : StrideApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    {
        Game = new Game();
        return base.FinishedLaunching(app, options);
    }

    static void Main(string[] args)
    {
        UIApplication.Main(args, null, "MyTemplateAppDelegate");
    }

    public override void WillTerminate(UIApplication application)
    {
        Game.Dispose();
    }
}
