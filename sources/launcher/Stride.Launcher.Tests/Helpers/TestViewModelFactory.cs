using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.ViewModels;

namespace Stride.Launcher.Tests.Helpers;

internal static class TestViewModelFactory
{
    internal static (MainViewModel Vm, InMemoryLauncherSettings Settings, FakeDialogService Dialog)
        CreateMainViewModel()
    {
        var settings = new InMemoryLauncherSettings();
        var dialog = new FakeDialogService();
        var serviceProvider = new ViewModelServiceProvider([dialog, settings]);
        var vm = new MainViewModel(serviceProvider, settings);
        return (vm, settings, dialog);
    }
}
