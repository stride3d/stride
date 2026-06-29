// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Cli.Core;

// Shared version resolution for the tool-launching commands.
internal static class CliVersion
{
    // Resolves the Stride version for the current directory (restoring the project first). Prints any error and
    // returns null on failure.
    public static PackageVersion? ResolveOrReport(StrideVersionManager manager, string? explicitVersion)
    {
        try
        {
            var resolved = manager.ResolveVersion(explicitVersion, Environment.CurrentDirectory);
            if (resolved is null)
                Console.Error.WriteLine("No Stride version is installed.");
            return resolved;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return null;
        }
    }
}
