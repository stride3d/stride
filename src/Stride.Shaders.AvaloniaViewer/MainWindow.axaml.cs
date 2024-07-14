using System.Collections.Generic;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace Stride.Shaders.AvaloniaViewer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var editor = this.FindControl<TextEditor>("ShaderEditor");
        var registry = new Registry(new RegistryOptions(ThemeName.Dark));
        var grammar = registry.LoadGrammarFromPathSync("./sdsl.tmLanguage.json", 0, []);

    }
}