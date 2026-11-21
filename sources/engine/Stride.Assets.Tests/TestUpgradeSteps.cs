// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Stride.Core;
using Xunit;
using static Stride.Assets.CodeUpgrades;

namespace Stride.Assets.Tests;

/// <summary>
/// The titles and IDs of the upgrade steps, which the upgrade report and the upgrade guide show.
/// </summary>
public class TestUpgradeSteps
{
    private static readonly SymbolRewrite UndescribedRewrite = new(_ => [], (_, _, _) => { });

    [Fact]
    public void SymbolRewritesDescribeTheirChange()
    {
        Assert.Equal("Call `Target.IsSRgb` as a method", PropertyToMethod("TestNs.Target", "IsSRgb").Description);
        Assert.Equal("Read `PixelFormatExtensions.IsSRgb` as a property", MethodToProperty("Stride.Graphics.PixelFormatExtensions", "IsSRgb").Description);
        Assert.Equal("Rename the parameter `count` of the `IndexBufferBinding` constructor to `indexCount`", ParameterRename("Stride.Graphics.IndexBufferBinding", ".ctor", "count", "indexCount").Description);
        Assert.Equal("Rename the parameter `align` of `Utilities.AllocateMemory` to `alignment`", ParameterRename("Stride.Core.Utilities", "AllocateMemory", "align", "alignment").Description);
        Assert.Equal("Move `Utilities.FreeMemory` to `MemoryUtilities.Free`", StaticMemberMove("Stride.Core.Utilities", "FreeMemory", "Stride.Core.MemoryUtilities", "Free").Description);
        Assert.Equal("Move `Utilities.Swap` to `MemoryUtilities.Swap`", StaticMemberMove("Stride.Core.Utilities", "Swap", "Stride.Core.MemoryUtilities").Description);
        Assert.Equal("Rename `CommandList.UnsetRenderTargets` to `ResetTargets`", MemberRename("Stride.Graphics.CommandList", "UnsetRenderTargets", "ResetTargets").Description);
        Assert.Equal("Remove the calls of `CommandList.ResetQueryPool`", FluentCallRemove("Stride.Graphics.CommandList", "ResetQueryPool").Description);
        Assert.Equal("Remove the assignments and subscriptions of `GraphicsDeviceManager.DeviceReset`", AssignmentRemove("Stride.Games.GraphicsDeviceManager", "DeviceReset").Description);
        Assert.Equal("Use `Game.Audio` as an extension member of `Stride.Audio`", MemberToExtension("Stride.Engine.Game", "Audio", "Stride.Audio").Description);
        Assert.Equal("Use `ScriptComponent.Audio` through `Game`, as an extension member of `Stride.Audio`", MemberToExtension("Stride.Engine.ScriptComponent", "Audio", "Stride.Audio", throughMember: "Game").Description);
    }

    [Fact]
    public void StepHelpersTitleTheirChange()
    {
        Assert.Equal("Add `CaptureState` to the classes that implement `IInputDevice`", InterfaceMemberAdd("Stride.Input.IInputDevice", "public DeviceCaptureState CaptureState { get; } = new DeviceCaptureState();", "Stride.Input").Title);
        Assert.Equal("Move the overrides of `GamePadDeviceBase.State` to `protected RawState`", PropertyOverrideRename("Stride.Input.GamePadDeviceBase", "State", "RawState", SyntaxKind.ProtectedKeyword).Title);
        Assert.Equal("Move the overrides of `TouchController.IsPressed` to `protected IsRawPressed`", MethodOverrideRename("Stride.VirtualReality.TouchController", "IsPressed", "IsRawPressed", SyntaxKind.ProtectedKeyword).Title);
        Assert.Equal("Remove the unused `using` directives of `SharpDX`, `Vortice`", RemoveUnusedUsings("SharpDX", "Vortice").Title);
        Assert.Equal("Add `[EffectKeys]` to the types that hold static `ParameterKey` fields", AddAttributeToStaticFieldHolders("Stride.Rendering.ParameterKey", "Stride.Rendering.EffectKeysAttribute").Title);
    }

    [Fact]
    public void ARewriteStepIsTitledByItsRewrites()
    {
        Assert.Equal("Remove the calls of `Buffer.RecreateWith`", Rewrite(FluentCallRemove("Stride.Graphics.Buffer", "RecreateWith")).Title);
        Assert.Equal("Remove the calls of `Buffer.RecreateWith`; Rename `CommandList.UnsetRenderTargets` to `ResetTargets`",
            Rewrite(FluentCallRemove("Stride.Graphics.Buffer", "RecreateWith"), MemberRename("Stride.Graphics.CommandList", "UnsetRenderTargets", "ResetTargets")).Title);
        Assert.Equal("Remove the device reset hooks", Rewrite("Remove the device reset hooks", FluentCallRemove("Stride.Graphics.Buffer", "RecreateWith")).Title);
    }

    [Fact]
    public void AStepWithoutTitleIsRefused()
    {
        Assert.Throws<ArgumentException>(() => Rewrite(UndescribedRewrite));
        Assert.Throws<ArgumentException>(() => Rewrite(" ", UndescribedRewrite));
        Assert.Equal("Titled", Rewrite("Titled", UndescribedRewrite).Title);

        Assert.Throws<ArgumentException>(() => Custom(null, (solution, _, _) => Task.FromResult(solution)));
        Assert.Throws<ArgumentException>(() => Custom("", (solution, _, _) => Task.FromResult(solution)));

        var registry = new UpgradeRegistry();
        Assert.ThrowsAny<ArgumentException>(() => registry.Project("4.5.0.0", null, _ => { }));
        Assert.ThrowsAny<ArgumentException>(() => registry.Project("4.5.0.0", " ", _ => { }));
        Assert.ThrowsAny<ArgumentException>(() => registry.RetiredProject("4.5.0.0", ""));
    }

    [Theory]
    [InlineData("4.5.0.0", "4.5.0")]
    [InlineData("4.5.1", "4.5.1")]
    [InlineData("4.5.0.2", "4.5.0.2")]
    public void AGateIsWrittenWithoutItsFourthPartWhenZero(string gate, string expected)
    {
        Assert.Equal(expected, UpgradeRegistry.FormatGate(new PackageVersion(gate).Version));
    }

    [Fact]
    public void StepsAreNumberedPerGateAndKind()
    {
        var registry = new UpgradeRegistry();
        registry.Code("4.5.0", [Retired("A"), Retired("B")]);
        registry.Project("4.5.0.0", "C", _ => { });
        registry.Code("4.5.1.0", [Retired("D")]);
        registry.Code("4.5.0.0", [Retired("E")]);
        registry.RetiredProject("4.5.0", "F");

        Assert.Equal(["My-4.5.0-C1", "My-4.5.0-C2", "My-4.5.1-C1", "My-4.5.0-C3", "My-4.5.0-P1", "My-4.5.0-P2"], registry.GetSteps("My").Select(x => x.Id));
    }

    private const string UpdateStepListVariable = "STRIDE_UPDATE_UPGRADE_STEPS";

    // The list of Stride's steps is checked in, so that a change of IDs shows in review; the upgrade guide is written from it.
    [Fact]
    public void StrideStepsMatchTheirList()
    {
        var registry = new UpgradeRegistry();
        new StridePackageUpgrader().DeclareUpgrades(registry);
        var steps = registry.GetSteps(StridePackageUpgrader.UpgraderName);
        var path = Path.Combine(CheckoutRoot(), "sources", "engine", "Stride.Assets", "StridePackageUpgrader.Steps.md");
        var list = WriteStepList(steps);
        if (Environment.GetEnvironmentVariable(UpdateStepListVariable) == "1")
        {
            File.WriteAllText(path, list);
            return;
        }

        var listed = File.ReadAllText(path).Replace("\r\n", "\n");
        var gates = steps.Select(x => Gate(x.Id)).ToHashSet();
        var ids = steps.Select(x => x.Id).ToHashSet();
        var removed = ReadStepIds(listed).Where(id => gates.Contains(Gate(id)) && !ids.Contains(id)).ToList();
        Assert.True(removed.Count == 0,
            $"Steps {string.Join(", ", removed)} were removed. A step is never removed from a gate: put Retired(title) or registry.RetiredProject(version, title) in its place, so that the IDs of the steps after it stay. Only a whole gate may go.");
        Assert.True(listed == list,
            $"The upgrade steps changed. Run this test with {UpdateStepListVariable}=1 to update {Path.GetFileName(path)}, and check in its diff that each existing ID keeps its step: steps are only appended to a gate.");
    }

    private static string WriteStepList(IEnumerable<DeclaredUpgradeStep> steps)
    {
        var builder = new StringBuilder();
        builder.Append($"""
            # Upgrade steps of Stride

            Written by `TestUpgradeSteps.StrideStepsMatchTheirList`: run it with `{UpdateStepListVariable}=1` to update this file. Steps are only appended to a gate: each ID keeps its step, a removed step is retired in place, and only a whole gate may go.

            | ID | Step |
            |---|---|

            """);
        foreach (var step in steps.OrderBy(x => new Version(UpgradeRegistry.FormatGate(x.GateVersion.Version))))
            builder.Append($"| `{step.Id}` | {(step.IsRetired ? "Retired: " : "")}{step.Title} |\n");
        return builder.ToString().Replace("\r\n", "\n");
    }

    private static IEnumerable<string> ReadStepIds(string list)
        => list.Split('\n').Where(x => x.StartsWith("| `", StringComparison.Ordinal)).Select(x => x[3..x.IndexOf('`', 3)]);

    // Stride-4.5.0-P2 -> Stride-4.5.0
    private static string Gate(string id) => id[..id.LastIndexOf('-')];

    private static string CheckoutRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "sources", "engine", "Stride.Assets", "StridePackageUpgrader.cs")))
                return current.FullName;
        }
        throw new InvalidOperationException($"Could not find StridePackageUpgrader.cs above {AppContext.BaseDirectory}.");
    }
}
