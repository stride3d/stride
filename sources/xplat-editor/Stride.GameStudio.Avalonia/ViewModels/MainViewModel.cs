using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private string? message;

    public MainViewModel()
    {
        AboutCommand = new AsyncRelayCommand(OnAbout, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        ExitCommand = new RelayCommand(OnExit, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        OpenCommand = new RelayCommand(OnOpen);
    }

    public string? Message
    {
        get => message;
        set => SetProperty(ref message, value);
    }

    public ICommand AboutCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand OpenCommand { get; }

    private async Task OnAbout()
    {
        // FIXME: hide implementation details through a dialog service
        var window = new AboutWindow();
        await window.ShowDialog(((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!);
    }

    private void OnExit()
    {
        ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).TryShutdown();
    }

    private void OnOpen()
    {
        Message = "Clicked on Open";
    }
}
