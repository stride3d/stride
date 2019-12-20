using Avalonia;
using Avalonia.Markup.Xaml;

namespace xunit.runner.xenko
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
