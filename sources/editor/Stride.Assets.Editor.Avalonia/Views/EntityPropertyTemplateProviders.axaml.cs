using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class EntityPropertyTemplateProviders : ResourceDictionary
{
    public EntityPropertyTemplateProviders()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
