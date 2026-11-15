// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Shaders.Parsing;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Byte-order marks at the start of a file are skipped (as DXC does), also a second one that some editors save:
/// reading the file removes only the first.
/// </summary>
public class LeadingByteOrderMarkTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-bom");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void PreprocessorSkipsLeadingByteOrderMarks()
    {
        var text = MonoGamePreProcessor.Run("﻿﻿shader S { float Value() { return 1; } };", "S.sdsl");

        Assert.DoesNotContain('﻿', text);
        Assert.Equal("  shader S", text[..10]);
    }

    [Fact]
    public void FileWithTwoByteOrderMarksCompiles()
    {
        // VL.Addons' GlowBaseAddon
        byte[] byteOrderMark = [0xEF, 0xBB, 0xBF];
        File.WriteAllBytes(Path.Combine(sourceDir.FullName, "TwoByteOrderMarks.sdsl"),
            [.. byteOrderMark, .. byteOrderMark, .. Encoding.UTF8.GetBytes("shader TwoByteOrderMarks\r\n{\r\n    float Value() { return 1; }\r\n};\r\n")]);

        new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL").LoadExternalBuffer("TwoByteOrderMarks", [], out _, out _, out _);
    }
}
