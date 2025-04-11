// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Crash;

internal enum CrashLocation
{
    Main,
    UnhandledException
}

internal record CrashReportArgs
{
    public required Exception Exception { get; set; }
    public required CrashLocation Location { get; set; }
    public string[] Logs { get; set; } = [];
    public string? ThreadName { get; set; }
}
