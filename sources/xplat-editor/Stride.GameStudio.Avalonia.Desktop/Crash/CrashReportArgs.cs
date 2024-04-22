// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.Avalonia.Desktop.Crash;

internal enum CrashLocation
{
    Main,
    UnhandledException
}

internal class CrashReportArgs
{
    public Exception Exception;
    public CrashLocation Location;
    public string[] Logs;
    public string? ThreadName;
}
