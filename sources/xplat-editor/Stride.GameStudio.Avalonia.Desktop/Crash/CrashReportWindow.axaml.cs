using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Stride.GameStudio.Avalonia.Desktop.Crash;

public partial class CrashReportWindow : Window
{
    public CrashReportWindow()
    {
        InitializeComponent();
    }

    public void InitializeComponent(bool loadXaml = true)
    {
        if (loadXaml)
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
