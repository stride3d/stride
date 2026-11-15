// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Yaml;

namespace Stride.Core.Design.Tests;

public class TestYamlFilePaths
{
    private static string? Rename(string text) => YamlFilePaths.Rewrite(text, (path, _) => path.Replace("Resources", "Art"));

    private static List<(string Path, bool IsSourceHash)> Visit(string text)
    {
        var paths = new List<(string, bool)>();
        YamlFilePaths.Rewrite(text, (path, isSourceHash) =>
        {
            paths.Add((path, isSourceHash));
            return null;
        });
        return paths;
    }

    [Fact]
    public void VisitsTaggedValuesAndSourceHashKeysInTextOrder()
    {
        const string text = """
            !Sheet
            Source: !file ../Resources/a.png
            Sprites:
                -   Texture: !file ../Resources/b.png
            Other: {File: !file ../Resources/c.png}
            ~SourceHashes:
                1234~../Resources/a.png: 5678
            """;
        Assert.Equal([("../Resources/a.png", false), ("../Resources/b.png", false), ("../Resources/c.png", false), ("../Resources/a.png", true)], Visit(text));
    }

    [Theory]
    [InlineData("Source: ../Resources/a.png")]
    [InlineData("Other: \"../Resources/a.png\"")]
    [InlineData("Other:\n    1234~../Resources/a.png: 5678")]
    [InlineData("Dir: !dir ../Resources")]
    [InlineData("Not YAML: [ !file ../Resources/a.png")]
    [InlineData("")]
    public void AnythingElseIsNotAPath(string text)
    {
        Assert.Empty(Visit(text));
        Assert.Null(Rename(text));
    }

    [Theory]
    [InlineData("Source: !file ../Resources/a.png", "Source: !file ../Art/a.png")]
    [InlineData("Source: !file \"../Resources/a b.png\"", "Source: !file \"../Art/a b.png\"")]
    [InlineData("Source: !file '../Resources/a.png'", "Source: !file '../Art/a.png'")]
    [InlineData("Other: {A: !file ../Resources/a.png, B: !file ../Resources/b.png}", "Other: {A: !file ../Art/a.png, B: !file ../Art/b.png}")]
    [InlineData("~SourceHashes:\n    1234~../Resources/my a.png: 5678", "~SourceHashes:\n    1234~../Art/my a.png: 5678")]
    public void OnlyThePathChanges(string text, string expected)
        => Assert.Equal(expected, Rename(text));

    [Fact]
    public void EveryOtherCharacterStays()
    {
        var mark = ((char)0xFEFF).ToString();
        var text = mark + "!Asset\r\n# a comment\r\nSource:    !file ../Resources/a.png   \nOdd:   [1,2]\r\n\r\n";
        Assert.Equal(text.Replace("Resources", "Art"), Rename(text));
    }

    [Fact]
    public void NullKeepsAPathAndTheRestChanges()
    {
        const string text = "A: !file ../Resources/a.png\nB: !file ../Resources/b.png";
        Assert.Equal("A: !file ../Resources/a.png\nB: !file ../Art/b.png", YamlFilePaths.Rewrite(text, (path, _) => path.EndsWith("b.png") ? path.Replace("Resources", "Art") : null));
    }

    [Fact]
    public void TheSamePathIsNoChange()
        => Assert.Null(YamlFilePaths.Rewrite("Source: !file ../Resources/a.png", (path, _) => path));

    [Fact]
    public void AnAliasIsNotChangedTwice()
    {
        const string text = "A: &name !file ../Resources/a.png\nB: *name";
        Assert.Equal("A: &name !file ../Art/a.png\nB: *name", Rename(text));
    }

    [Theory]
    [InlineData("Source: !file \"..\\\\Resources\\\\a.png\"", "Source: !file \"../Art/a.png\"")]
    [InlineData("Source: !file \"../R\\xE9sources/a.png\"", "Source: !file \"../Art/a.png\"")]
    [InlineData("~SourceHashes:\n    \"1234~../R\\xE9sources/a.png\": 5678", "~SourceHashes:\n    \"1234~../Art/a.png\": 5678")]
    public void APathWrittenWithAnEscapeChanges(string text, string expected)
        => Assert.Equal(expected, YamlFilePaths.Rewrite(text, (_, _) => "../Art/a.png"));

    [Theory]
    [InlineData("Source: !file ../Resources/a.png")]
    [InlineData("Source: !file \"../Resources/a.png\"")]
    [InlineData("Source: !file '../Resources/a.png'")]
    [InlineData("A: &name !file ../Resources/a.png")]
    [InlineData("Other: {A: !file ../Resources/a.png, B: 1}")]
    [InlineData("Other: [!file ../Resources/a.png, 1]")]
    [InlineData("~SourceHashes:\n    1234~../Resources/a.png: 5678")]
    [InlineData("~SourceHashes: {1234~../Resources/a.png: 5678}")]
    public void ANewPathReadsBackAsItIsWherever(string text)
    {
        // Each of these breaks the text or changes the value if written as it is, in at least one form above.
        foreach (var newPath in new[] { "../Art #2/a.png", "../Art: x/a.png", "#a.png", "[a].png", "{a}.png", "!a.png", "&a.png", "*a.png", "%a.png", "@a.png", "`a.png", "- a.png", "? a.png", "a,b.png", "it's.png", "'a'.png", "say \"a\".png", "a\\b.png", "../Résumé/a.png", " a.png", "a.png " })
        {
            var rewritten = YamlFilePaths.Rewrite(text, (_, _) => newPath);
            Assert.NotNull(rewritten);
            Assert.All(Visit(rewritten), found => Assert.Equal(newPath, found.Path));
            Assert.NotEmpty(Visit(rewritten));
        }
    }

    [Theory]
    [InlineData("Source: !file a.png", "b.png", "Source: !file b.png")]
    [InlineData("Source: !file a.png", "../Art #2/a.png", "Source: !file '../Art #2/a.png'")]
    [InlineData("Other: {A: !file a.png, B: 1}", "a,b.png", "Other: {A: !file 'a,b.png', B: 1}")]
    [InlineData("Source: !file 'a.png'", "it's.png", "Source: !file 'it''s.png'")]
    [InlineData("~SourceHashes:\n    1234~a.png: 5678", "b #2.png", "~SourceHashes:\n    '1234~b #2.png': 5678")]
    public void QuotesAreAddedOnlyWhenNeeded(string text, string newPath, string expected)
        => Assert.Equal(expected, YamlFilePaths.Rewrite(text, (_, _) => newPath));

    [Theory]
    [InlineData("Source: !file ../Resources/\n    a.png")]
    [InlineData("!file a\nb\nc\nd\ne\nf\ng\nh\ni\nj")]
    [InlineData("Source: !file |\n    ../Resources/a.png\n")]
    [InlineData("Source: !file >-\n    ../Resources/a.png\n")]
    public void APathOnSeveralLinesIsAnErrorOnlyWhenItChanges(string text)
    {
        Assert.Throws<InvalidOperationException>(() => YamlFilePaths.Rewrite(text, (_, _) => "../Art/a.png"));
        Assert.Null(YamlFilePaths.Rewrite(text, (_, _) => null));
    }
}
