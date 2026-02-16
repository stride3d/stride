using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using Silk.NET.Shaderc;
using Silk.NET.SPIRV.Cross;
using Stride.Shaders.Compilers;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;



namespace Stride.Shaders.AvaloniaViewer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var editor = this.FindControl<TextEditor>("ShaderEditor") ?? throw new NotImplementedException();
        editor.Text = @"struct PSInput
{
	float4 color : COLOR;
};

float4 PSMain(PSInput input) : SV_TARGET
{
	return input.color;
}
";
        var options = new RegistryOptions(ThemeName.Dark);
        //Initial setup of TextMate.
        var textMateInstallation = editor.InstallTextMate(options);

        textMateInstallation.SetGrammar(options.GetScopeByLanguageId(options.GetLanguageByExtension(".hlsl").Id));


        
    }

    public void Recompile(object source, EventArgs args)
    {
        var editor = this.FindControl<TextEditor>("ShaderEditor") ?? throw new NotImplementedException();
        var other = this.FindControl<TextEditor>("OutputEditor") ?? throw new NotImplementedException();
        if(string.IsNullOrEmpty(editor.Text)) 
            editor.Text = @"struct PSInput
{
	float4 color : COLOR;
};

float4 PSMain(PSInput input) : SV_TARGET
{
	return input.color;
}
";
        try
        {
            other.Text = SpirvOptimizer.Translate(editor.Text, "PSMain", SourceLanguage.Hlsl, Backend.Glsl);
        }
        catch(Exception e)
        {
            other.Text = e.Message;
        }
        finally
        {
            other.Text = "";
        }

    }


}