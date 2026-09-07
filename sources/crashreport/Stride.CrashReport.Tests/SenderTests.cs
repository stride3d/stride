// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Xunit;

namespace Stride.CrashReport.Tests;

public class SenderTests
{
    [Theory]
    [InlineData("GameStudio", null, new[] { "System.InvalidOperationException" }, "[GameStudio] InvalidOperationException")]
    [InlineData("AssetCompiler", "ModelAsset", new[] { "System.NotSupportedException" }, "[AssetCompiler] NotSupportedException (ModelAsset)")]
    [InlineData("AssetCompiler", "ModelAsset", new[] { "NativeCrash" }, "[AssetCompiler] NativeCrash (ModelAsset)")]
    // Wrappers are unwrapped for the title only; the chain is listed innermost-first.
    [InlineData("GameStudio", null, new[] { "System.NullReferenceException", "System.Reflection.TargetInvocationException" }, "[GameStudio] NullReferenceException")]
    [InlineData("GameStudio", null, new[] { "System.IO.IOException", "System.AggregateException" }, "[GameStudio] IOException")]
    [InlineData("GameStudio", null, new[] { "System.NullReferenceException", "Stride.Core.Assets.AssetException" }, "[GameStudio] AssetException")]
    [InlineData("Launcher", null, new[] { "System.Collections.Generic.KeyNotFoundException`1" }, "[Launcher] KeyNotFoundException")]
    public void TitleNamesAppKindAndAssetType(string application, string? assetType, string[] chainInnermostFirst, string expected)
        => Assert.Equal(expected, CrashReportSender.Title(application, CrashReportSender.TitleKind(chainInnermostFirst), assetType));

    [Theory]
    [InlineData("Stride.Core.BuildEngine.CommandBuildStep+<StartCommand>d__30", "MoveNext", "Stride.Core.BuildEngine.CommandBuildStep.StartCommand")]
    [InlineData("Stride.Core.BuildEngine.CommandBuildStep", "Execute", "Stride.Core.BuildEngine.CommandBuildStep.Execute")]
    [InlineData("Stride.Core.BuildEngine.Builder+<>c__DisplayClass81_2", "<ScheduleBuildStep>b__0", "Stride.Core.BuildEngine.Builder+<>c__DisplayClass81_2.<ScheduleBuildStep>b__0")]
    [InlineData(null, "Main", "Main")]
    public void FrameNamesFoldAsyncStateMachines(string? type, string method, string expected)
        => Assert.Equal(expected, FrameNames.Qualified(type, method));

    [Fact]
    public async Task SendToUnreachableDsnThrowsSoTheReportIsKept()
    {
        // Every sender deletes a report once "sent". The SDK only logs a transport failure, so the sender must
        // turn an upload that never landed into an exception, or an offline user's report would be lost silently.
        var crash = StoredCrash.FromReportData(new CrashReportData { ["Application"] = "TestProbe", ["Exception"] = "boom" });
        crash.Application = "TestProbe";
        crash.Version = "0.0.0-test";
        crash.Signature = "test";

        // Port 9 (discard) on loopback: the connection is refused at once, no network needed.
        var exception = await Assert.ThrowsAsync<IOException>(() => CrashReportSender.SendAsync(crash, dump: null, dsn: "https://0123456789abcdef0123456789abcdef@127.0.0.1:9/1"));
        Assert.StartsWith("Crash report upload", exception.Message);
    }
}
