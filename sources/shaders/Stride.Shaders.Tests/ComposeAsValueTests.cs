// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A composition is a shader, not a value: using it as one (<c>lerp(Color, ...)</c>, <c>float3(Hue, 0, 0)</c>) gets a clear error
/// that says to call its method (SDSL0165), not an internal one.
/// </summary>
public class ComposeAsValueTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-compose-value");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string Value = """
        namespace Stride.Shaders.Tests;

        shader ComposeAsValueSource
        {
            float Compute() { return 0.5; }
        }
        """;

    [Theory]
    // VL.Addons' TransformColor_Advanced_Internal_ShaderFX and ShiftRGB_Internal_TextureFX
    [InlineData("return lerp(Hue, 1.0, 0.5);", "Hue", "Hue.Compute()")]
    [InlineData("return float3(Hue, 0, 0).x;", "Hue", "Hue.Compute()")]
    [InlineData("return Hue + 1.0;", "Hue", "Hue.Compute()")]
    [InlineData("return -Hue;", "Hue", "Hue.Compute()")]
    [InlineData("return Hue;", "Hue", "Hue.Compute()")]
    [InlineData("float x = Hue; return x;", "Hue", "Hue.Compute()")]
    [InlineData("return Twice(Hue);", "Hue", "Hue.Compute()")]
    [InlineData("return Hues + 1.0;", "Hues", "Hues[0].Compute()")]
    [InlineData("return Hues[1] + 1.0;", "Hues[1]", "Hues[1].Compute()")]
    [InlineData("float x = 0; foreach (var h in Hues) x += h; return x;", "h", "h.Compute()")]
    public void CompositionUsedAsAValueSaysToCallIt(string body, string composition, string call)
    {
        var log = Compile(body);
        Assert.True(log.Contains($"ComposeAsValue.sdsl(12,"), log);
        Assert.True(log.Contains($"SDSL0165: composition {composition} is used as a value, but a composition is a shader. Call its method instead, for example {call}."), log);
    }

    [Theory]
    [InlineData("return lerp(Hue.Compute(), 1.0, 0.5);")]
    [InlineData("return Hues[1].Compute() + Hue.Compute();")]
    [InlineData("float x = 0; foreach (var h in Hues) x += h.Compute(); return x;")]
    public void CallingTheCompositionIsFine(string body)
    {
        var log = Compile(body);
        Assert.True(log.Length == 0, log);
    }

    private string Compile(string body)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "ComposeAsValueSource.sdsl"), Value);
        File.WriteAllText(Path.Combine(sourceDir.FullName, "ComposeAsValue.sdsl"), $$"""
            namespace Stride.Shaders.Tests;

            shader ComposeAsValue
            {
                compose ComposeAsValueSource Hue;
                compose ComposeAsValueSource Hues[];

                float Twice(float x) { return x * 2; }

                float Shade()
                {
                    {{body}}
                }
            }
            """);

        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };
        try
        {
            loader.LoadExternalBuffer("ComposeAsValue", [], out _, out _, out _);
        }
        catch (Exception e)
        {
            return e.Message;
        }
        return string.Join(Environment.NewLine, log.Messages.Where(m => m.Type == LogMessageType.Error).Select(m => m.Text));
    }
}
