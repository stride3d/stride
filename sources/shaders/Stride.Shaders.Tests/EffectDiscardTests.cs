// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using Stride.Shaders.Tests;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// <c>discard;</c> in an effect stops the effect where it is written (asset <c>CompilerTests/EffectDiscard.sdfx</c>).
/// The editor's error fallback (<c>StrideEditorForwardShadingEffect</c>) depends on it.
/// </summary>
public class EffectDiscardTests
{
    [Fact]
    public void DiscardStopsTheEffect()
    {
        var parameters = new ParameterCollection();
        parameters.Set(EffectDiscardParameters.IsError, true);

        var mixin = ShaderMixinManager.Generate("EffectDiscard", parameters);

        Assert.Equal(["ErrorShader"], mixin.Mixins.Select(x => x.ClassName));
    }

    [Fact]
    public void NoDiscardRunsTheWholeEffect()
    {
        var mixin = ShaderMixinManager.Generate("EffectDiscard", new ParameterCollection());

        Assert.Equal(["FullShader"], mixin.Mixins.Select(x => x.ClassName));
    }
}
