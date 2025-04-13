// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.Avalonia.Helpers;

public class StrideGameStudio
{
    public static string EditorVersion => StrideVersion.NuGetVersion;
    public static string EditorVersionMajor => new Version(StrideVersion.PublicVersion).ToString(2);
    public static string AnswersUrl => "https://gamedev.stackexchange.com/tags/stride"; // #706
    public static string DocumentationUrl => $"https://doc.stride3d.net/{EditorVersionMajor}/";
    public static string ForumsUrl => "https://github.com/stride3d/stride/discussions";
    public static string ReportIssueUrl => "https://github.com/stride3d/stride/issues/";
}
