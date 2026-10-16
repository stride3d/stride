// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Tests;

namespace Stride.Shaders.Parsers.Tests;

public class BoolOperatorTests
{
    // DXC's -Wparentheses: only the unparenthesized comparison inside &, | or ^ warns
    [Fact]
    public void ComparisonInBitwiseOperationIsAWarning()
    {
        var loader = new ShaderLoader("./assets/SDSL/RenderTests") { Log = new LoggerResult() };
        loader.LoadExternalBuffer("BoolBitwise", [], out _, out _, out _);

        var warnings = ((LoggerResult)loader.Log).Messages.Where(m => m.Type == LogMessageType.Warning).Select(m => m.Text).ToList();
        var warning = Assert.Single(warnings);
        Assert.Contains("SDSL0232: & has lower precedence than >: y > border & !b computes y > border first. Add parentheses to make it clear.", warning);
    }
}
