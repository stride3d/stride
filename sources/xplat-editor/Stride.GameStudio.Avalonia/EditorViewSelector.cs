using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.GameStudio.Avalonia;

// TODO xplat-editor consider moving this class to Stride.Assets.Editor.Avalonia
public sealed class EditorViewSelector : AvaloniaObject, IDataTemplate
{
    private ISessionViewModel? session;

    public static readonly DirectProperty<EditorViewSelector, ISessionViewModel?> SessionProperty =
        AvaloniaProperty.RegisterDirect<EditorViewSelector, ISessionViewModel?>(nameof(Session), o => o.Session, (o, v) => o.Session = v);

    public ISessionViewModel? Session
    {
        get => session;
        set => SetAndRaise(SessionProperty, ref session, value);
    }

    public Control? Build(object? param)
    {
        if (param == null) return null;

        var viewType = Session?.ServiceProvider.TryGet<IAssetsPluginService>()?.GetEditorViewType(param.GetType());
        return viewType != null ? Activator.CreateInstance(viewType) as Control : null;
    }

    public bool Match(object? data) => data is AssetEditorViewModel;
}
