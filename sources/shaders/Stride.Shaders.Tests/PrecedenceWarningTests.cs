// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Shaders.Parsers.Tests;

public class PrecedenceWarningTests
{
    // DXC's -Wparentheses: & within |, && within ||, + or - within a shift; not with parentheses, ^, or one kind of operator
    [Fact]
    public void OperatorMixesWithoutParenthesesAreWarnings()
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests") { Log = new LoggerResult() };
        loader.LoadExternalBuffer("PrecedenceWarnings", [], out _, out _, out _);

        var warnings = ((LoggerResult)loader.Log).Messages.Where(m => m.Type == LogMessageType.Warning).Select(m => m.Text).ToList();
        Assert.Equal(4, warnings.Count);
        Assert.Contains(warnings, w => w.Contains("SDSL0242: & within |: c | a & b computes a & b first. Add parentheses around it to make it clear."));
        Assert.Contains(warnings, w => w.Contains("SDSL0243: && within ||: x && y || z computes x && y first. Add parentheses around it to make it clear."));
        Assert.Contains(warnings, w => w.Contains("SDSL0244: << has lower precedence than +: a << b + 1 computes b + 1 first. Add parentheses to make it clear."));
        Assert.Contains(warnings, w => w.Contains("SDSL0244: >> has lower precedence than -: a >> c - b computes c - b first. Add parentheses to make it clear."));
    }
}
