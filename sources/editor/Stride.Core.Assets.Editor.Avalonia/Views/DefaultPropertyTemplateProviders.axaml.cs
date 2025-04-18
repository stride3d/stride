using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class DefaultPropertyTemplateProviders : ResourceDictionary
{
    public DefaultPropertyTemplateProviders()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
