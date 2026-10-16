// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.BuildEngine.Tests.Commands;
using Stride.Core.Diagnostics;
using Xunit;

namespace Stride.Core.BuildEngine.Tests;

public class TestLogs
{
    [Fact]
    public void NestedListLogsAreForwardedToParent()
    {
        Utils.CleanContext();
        var builder = Utils.CreateBuilder(false);

        // Same shape as an asset compiler that nests an AssetBuildStep in its result: the editor shows the log of the outer list
        var outer = new ListBuildStep();
        var inner = new ListBuildStep();
        inner.Add(new ExceptionCommand());
        outer.Add(inner);
        builder.Root.Add(outer);

        builder.Run(Builder.Mode.Build);

        Assert.Equal(ResultStatus.Failed, outer.Status);
        Assert.Contains(outer.Logger.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains(nameof(System.NotImplementedException)));
    }
}
