// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Upgrade;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// The upgrade mode of the parser: the upgrade reads an old spelling marked <see cref="RecoverableInUpgradeAttribute"/> as its new form.
/// </summary>
public class ShaderUpgradeParseModeTests
{
    // Each marked code: MyHelpers with the old spelling, and with the new form
    public static TheoryData<string, string, string> OldSpellings() => new()
    {
        { "SDSL0045", "shader MyHelpers\n{\n    stage float Scale;\n};\n", "shader MyHelpers\n{\n    module float Scale;\n};\n" },
        { "SDSL0051", "class MyHelpers\n{\n    module float Scale;\n};\n", "shader MyHelpers\n{\n    module float Scale;\n};\n" },
    };

    private const string User = "shader MyUser : MyHelpers\n{\n    float Get() { return Scale; }\n};\n";

    [Fact]
    public void EveryMarkedCodeHasAnOldSpelling()
    {
        var marked = typeof(SDSLErrorMessages).GetFields().Where(x => x.IsDefined(typeof(RecoverableInUpgradeAttribute), false)).Select(x => x.Name);
        Assert.Equal(marked.Order(), OldSpellings().Select(x => (string)x.Data.Item1).Order());
    }

    // A build reports the old spelling; the upgrade reads past it, with the error in Recovered
    [Theory]
    [MemberData(nameof(OldSpellings))]
    public void TheUpgradeModeRecoversOnlyTheMarkedError(string code, string oldForm, string newForm)
    {
        Assert.Contains(SDSLParser.Parse(oldForm).Errors, x => x.Message.StartsWith($"{code}:", StringComparison.Ordinal));
        var upgrade = SDSLParser.Parse(oldForm, upgrade: true);
        Assert.Empty(upgrade.Errors);
        Assert.Contains(upgrade.Recovered, x => x.Message.StartsWith($"{code}:", StringComparison.Ordinal));
        Assert.Empty(SDSLParser.Parse(newForm).Errors);
    }

    // A step that reads the classes decides the same for both: MyHelpers has no instance part and MyUser inherits it, so MyHelpers becomes
    // a module and MyUser imports it. The step's text edits inside MyHelpers can differ: the old spelling stays for a text step
    [Theory]
    [MemberData(nameof(OldSpellings))]
    public void AnOldSpellingLeavesTheSameClassAsTheNewForm(string code, string oldForm, string newForm)
    {
        var fromOld = ShaderUpgrader.Upgrade(new Dictionary<string, string> { ["MyHelpers.sdsl"] = oldForm, ["MyUser.sdsl"] = User }, engine: null, steps: ["module-units"]);
        var fromNew = ShaderUpgrader.Upgrade(new Dictionary<string, string> { ["MyHelpers.sdsl"] = newForm, ["MyUser.sdsl"] = User }, engine: null, steps: ["module-units"]);

        Assert.NotEqual(User, fromNew.Files["MyUser.sdsl"]);
        Assert.Equal(fromNew.Files["MyUser.sdsl"], fromOld.Files["MyUser.sdsl"]);
        Assert.StartsWith("module MyHelpers\n", fromNew.Files["MyHelpers.sdsl"], StringComparison.Ordinal);
        Assert.StartsWith("module MyHelpers\n", fromOld.Files["MyHelpers.sdsl"], StringComparison.Ordinal);
        Assert.Contains(fromOld.Notes, x => x.Contains($"{code}:", StringComparison.Ordinal));
    }
}
