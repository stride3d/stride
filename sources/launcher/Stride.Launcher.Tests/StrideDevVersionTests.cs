// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Launcher.ViewModels;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class StrideDevVersionTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("stride-ledger-tests").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Checkout(string name) => Directory.CreateDirectory(Path.Combine(root, name)).FullName;

    [Fact]
    public void FindCheckoutInLedger_MapsTheDevSuffix()
    {
        string[] ledger = ["# Stride checkout version-suffix ledger", $"dev = {Checkout("stride")}", $"dev2 = {Checkout("stride2")}", $"dev3 = {Checkout("stride3")}"];

        Assert.Equal(Path.Combine(root, "stride3"), StrideDevVersionViewModel.FindCheckoutInLedger(ledger, "beta8-dev3"));
        Assert.Equal(Path.Combine(root, "stride"), StrideDevVersionViewModel.FindCheckoutInLedger(ledger, "beta8-dev"));
    }

    [Fact]
    public void FindCheckoutInLedger_PrimaryIsDev()
    {
        string[] ledger = [$"(primary) = {Checkout("stride")}"];

        Assert.Equal(Path.Combine(root, "stride"), StrideDevVersionViewModel.FindCheckoutInLedger(ledger, "dev"));
    }

    [Fact]
    public void FindCheckoutInLedger_IgnoresCaseAndSpaces()
    {
        string[] ledger = [$"  DEV2   =   {Checkout("stride2")}  "];

        Assert.Equal(Path.Combine(root, "stride2"), StrideDevVersionViewModel.FindCheckoutInLedger(ledger, "beta8-dev2"));
    }

    [Fact]
    public void FindCheckoutInLedger_SkipsCommentsAndMissingFolders()
    {
        string[] ledger = [$"# dev3 = {Checkout("commented")}", $"dev3 = {Path.Combine(root, "missing")}", "not a ledger line"];

        Assert.Null(StrideDevVersionViewModel.FindCheckoutInLedger(ledger, "beta8-dev3"));
    }

    [Fact]
    public void FindCheckoutInLedger_NoDevSuffix_IsNull()
    {
        string[] ledger = [$"dev = {Checkout("stride")}"];

        Assert.Null(StrideDevVersionViewModel.FindCheckoutInLedger(ledger, "beta8"));
    }
}
