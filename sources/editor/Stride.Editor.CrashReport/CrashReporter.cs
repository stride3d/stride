// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;

namespace Stride.Editor.CrashReport;

internal static class CrashReporter
{
    private const string url = "https://github.com/stride3d/stride/issues/new?labels=bug&template=bug_report.md&";
    
    internal static void OpenGithub()
    {
        Process browser = new();
        browser.StartInfo.UseShellExecute = true; 
        browser.StartInfo.FileName = url;
        browser.Start();
    }
}