// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Parsing;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Editors can save a file with no newline after its last line, also when that line is a preprocessor directive.
/// </summary>
public class NoFinalNewlineTests
{
    [Theory]
    [InlineData("#ifdef VALUE\nshader S { };\n#endif")]
    [InlineData("#ifdef VALUE\nshader S { };\n#endif // VALUE")]
    [InlineData("#ifdef VALUE\nshader S { };\n#endif\n#define LAST 1")]
    [InlineData("#ifdef VALUE\nshader S { };\n#endif\n// The end")]
    [InlineData("#ifdef VALUE\nshader S { };\n#endif\r\n")]
    public void LastLineNeedsNoNewline(string code)
    {
        var text = MonoGamePreProcessor.Run(code, "S.sdsl", ("VALUE", "1"));

        Assert.Contains("shader S { };", text);
    }
}
