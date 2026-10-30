using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.Avalonia.ViewModels;

internal sealed class MainViewModel : ViewModelBase
{
#if DEBUG
    // Note: only required for the Avalonia designer
    public MainViewModel()
        : this(ViewModelServiceProvider.NullServiceProvider)
    { }
#endif

    public MainViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public string Title
    {
        get;
        set => SetValue(ref field, value);
    } = "Stride Game Studio";
}
