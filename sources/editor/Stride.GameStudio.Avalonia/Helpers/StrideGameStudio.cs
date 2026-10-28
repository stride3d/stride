// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Stride.GameStudio.Avalonia.Helpers;

public static class StrideGameStudio
{
    public static string CopyrightText1 => "© 2018 Stride contributors";
    public static string CopyrightText2 => "© 2011-2018 Silicon Studio Corp.";
    /// <summary>The editor name, as the editor layers know it (the build cache folder, the dialogs' title).</summary>
    public static string EditorName => Assets.Editor.StrideEditorPlugin.EditorName;
    public static string EditorTitle => $"{EditorName} {EditorVersion} ({RuntimeLabel})";
    /// <summary>".NET 10.0.0-rc.1.26425.128" -> ".NET 10.0.0-rc.1": the build number is too long for a title.</summary>
    public static string RuntimeLabel
    {
        get
        {
            var match = Regex.Match(RuntimeInformation.FrameworkDescription, @"^\.NET \d+\.\d+\.\d+(-[a-z]+\.\d+)?");
            return match.Success ? match.Value : RuntimeInformation.FrameworkDescription;
        }
    }
    public static string EditorVersion => StrideVersion.NuGetVersion;
    public static string EditorVersionWithMetadata => StrideVersion.NuGetVersion + StrideVersion.BuildMetadata;
    public static string EditorVersionMajor => new Version(StrideVersion.PublicVersion).ToString(2);
    public static string AnswersUrl => "https://github.com/stride3d/stride/discussions";
    public static string DocumentationUrl => $"https://doc.stride3d.net/{EditorVersionMajor}/";
    public static string ForumsUrl => "https://discord.gg/f6aerfE";
    public static string ReportIssueUrl => "https://github.com/stride3d/stride/issues/";
}
