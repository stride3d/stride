// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Settings;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestEditorSettingsDeclaration
{
    private static readonly SettingsContainer Container = new();

    [EditorSettings]
    internal static class DeclaredSettings
    {
        public static readonly SettingsKey<bool> FieldKey = new("Test/Declared/Field", Container, true);

        public static SettingsKey<int> PropertyKey { get; } = new("Test/Declared/Property", Container, 3);
    }

    [Fact]
    public void KeysOfADeclaredClassAreFoundOnce()
    {
        var keys = EditorSettingsAttribute.GetKeys(typeof(DeclaredSettings)).ToList();

        Assert.Equal(2, keys.Count);
        Assert.Contains(DeclaredSettings.FieldKey, keys);
        Assert.Contains(DeclaredSettings.PropertyKey, keys);
    }

    [Fact]
    public void DeclaredKeysComeFromTheScanIndex()
    {
        var keys = EditorSettingsAttribute.GetDeclaredKeys(typeof(TestEditorSettingsDeclaration).Assembly).ToList();

        Assert.Contains(DeclaredSettings.FieldKey, keys);
        Assert.Contains(DeclaredSettings.PropertyKey, keys);
    }
}
