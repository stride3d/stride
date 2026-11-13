// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A stage method that reads instance state gets a notice: its shader is fully imported in a composition.
/// Stage cbuffer members and constants are no instance state.
/// </summary>
public class StageAccessNoticeTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-stage-notice");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void OnlyNonStageStateGetsANotice()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "NoticeBase.sdsl"), """
            shader NoticeBase
            {
                cbuffer PerView { stage float4 Tint; };
                cbuffer PerMaterial { float Gain; };
                static const float Half = 0.5;
            };
            """);
        File.WriteAllText(Path.Combine(sourceDir.FullName, "NoticeChild.sdsl"), """
            shader NoticeChild : NoticeBase
            {
                cbuffer PerDraw { stage float OwnScale; };
                static const float OwnHalf = 0.5;
                stage float4 Shade() { return Tint * Half * NoticeBase.Half * OwnScale * OwnHalf; }
                stage float Boost() { return Gain; }
            };
            """);
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };

        loader.LoadExternalBuffer("NoticeChild", [], out _, out _, out _);

        var notice = Assert.Single(log.Messages).Text;
        Assert.Contains("Stage method 'NoticeChild.Boost' references non-stage variable 'NoticeBase.Gain'.", notice);
    }
}
