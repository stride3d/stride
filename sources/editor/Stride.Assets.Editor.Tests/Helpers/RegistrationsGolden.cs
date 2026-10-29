// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

#nullable enable

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// Compares the registrations of a plugin with the ones recorded in a file next to the test, so that a change of how a plugin
/// registers (the plugin context, a refactoring) shows the registrations it adds or loses.
/// </summary>
/// <remarks>
/// A missing file is written and the comparison fails, asking to check it in. Set <c>STRIDE_UPDATE_REGISTRATIONS=1</c> to
/// write every file again.
/// </remarks>
public static class RegistrationsGolden
{
    public const string UpdateVariable = "STRIDE_UPDATE_REGISTRATIONS";

    /// <summary>
    /// The difference between <paramref name="actual"/> and the recorded registrations named <paramref name="name"/>, in the
    /// <c>Registrations</c> folder next to the calling test file; null when they are the same.
    /// </summary>
    public static string? Compare(string name, string actual, [CallerFilePath] string testFile = "")
    {
        var directory = Path.Combine(Path.GetDirectoryName(testFile)!, "Registrations");
        var path = Path.Combine(directory, name + ".txt");
        actual = Normalize(actual);
        if (!File.Exists(path) || Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, actual);
            return $"The registrations of {name} were recorded in {path}: check them in.";
        }

        var expected = Normalize(File.ReadAllText(path));
        if (expected == actual)
            return null;

        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var lost = expectedLines.Except(actualLines).Select(x => "- " + x);
        var added = actualLines.Except(expectedLines).Select(x => "+ " + x);
        var difference = string.Join(Environment.NewLine, lost.Concat(added));
        return $"The registrations of {name} differ from {path} (the order may differ too):{Environment.NewLine}{difference}{Environment.NewLine}Now:{Environment.NewLine}{actual}";
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
}
