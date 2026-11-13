// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Cli.Core;
using Stride.Core;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class NewTemplateRulesTests
{
    private static readonly PackageVersion Newest = new("4.5.0-dev18");
    private static readonly PackageVersion Project = new("4.4.1");

    [Theory]
    [InlineData("item", true)]
    [InlineData("ITEM", true)]
    [InlineData("project", false)]
    public void TypeTagNamesAnItemTemplate(string type, bool expected)
        => Assert.Equal(expected, NewTemplateRules.IsItemTemplate(new Dictionary<string, string> { ["type"] = type }));

    [Fact]
    public void NoTypeTagIsNotAnItemTemplate()
        => Assert.False(NewTemplateRules.IsItemTemplate(new Dictionary<string, string> { ["language"] = "C#" }));

    [Fact]
    public void ItemTemplateInAProjectUsesTheProjectVersion()
        => Assert.Equal(Project, NewTemplateRules.ChooseVersion(null, true, Newest, () => Project));

    [Fact]
    public void ItemTemplateWithNoProjectUsesTheNewestVersion()
        => Assert.Equal(Newest, NewTemplateRules.ChooseVersion(null, true, Newest, () => null));

    [Fact]
    public void ExplicitVersionWinsWithoutLookingAtTheProject()
        => Assert.Equal(Newest, NewTemplateRules.ChooseVersion(Newest.ToString(), true, Newest, () => throw new InvalidOperationException("The project must not be restored.")));

    [Fact]
    public void ProjectTemplateDoesNotLookAtTheProject()
        => Assert.Equal(Newest, NewTemplateRules.ChooseVersion(null, false, Newest, () => throw new InvalidOperationException("The project must not be restored.")));

    [Fact]
    public void ItemTemplateNeverUpgradesAndDoesNotReadTheUserProject()
        => Assert.Null(NewTemplateRules.VersionToUpgradeFrom(true, Newest, () => throw new InvalidOperationException("The user's project must not be read.")));

    [Fact]
    public void ProjectTemplateOnAnOlderEngineUpgrades()
        => Assert.Equal("4.4.0", NewTemplateRules.VersionToUpgradeFrom(false, Newest, () => "4.4.0"));

    [Theory]
    [InlineData("4.5.0-dev18")]
    [InlineData("4.5.0")]
    [InlineData(null)]
    public void ProjectTemplateOnTheSameOrANewerEngineStaysAsItIs(string? generated)
        => Assert.Null(NewTemplateRules.VersionToUpgradeFrom(false, Newest, () => generated));
}
