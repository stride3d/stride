// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Text.RegularExpressions;

namespace Stride.Editor.CrashReport;

/// <summary>
/// Strips the user name and profile path from crash report text before it leaves the machine.
/// It also makes paths easier to copy and paste between machines.
/// </summary>
public static class CrashReportAnonymizer
{
    public static void Scrub(CrashReportData report)
    {
        for (var i = 0; i < report.Data.Count; i++)
        {
            report.Data[i] = (report.Data[i].Item1, Scrub(report.Data[i].Item2));
        }
    }

    public static string Scrub(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string.IsNullOrEmpty(userProfile))
            text = Regex.Replace(text, Regex.Escape(userProfile), "%USERPROFILE%", RegexOptions.IgnoreCase);

        var userName = Environment.GetEnvironmentVariable("USERNAME");
        if (!string.IsNullOrEmpty(userName))
            text = Regex.Replace(text, $@"\b{Regex.Escape(userName)}\b", "%USERNAME%", RegexOptions.IgnoreCase);

        return text;
    }
}
