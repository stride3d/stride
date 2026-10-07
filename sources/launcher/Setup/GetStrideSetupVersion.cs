// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// MSBuild inline task (RoslynCodeTaskFactory), used by Stride.Launcher.Release.targets

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

/// <summary>
/// StrideSetup's MSI version and ProductCode for a launcher version.
/// </summary>
public class GetStrideSetupVersion : Task
{
    /// <summary>The launcher version, e.g. 6.0.1 or 6.0.1-beta1.</summary>
    [Required]
    public string Version { get; set; }

    /// <summary>The MSI version, e.g. 6.0.199 or 6.0.131.</summary>
    [Output]
    public string MsiVersion { get; set; }

    /// <summary>The ProductCode, e.g. {886E469F-B132-5811-BF4B-9049A12F5A08} for 6.0.1.</summary>
    [Output]
    public string ProductCode { get; set; }

    public override bool Execute()
    {
        // NuGet compares "beta10" before "beta2" (text), so N stays a single digit
        var match = Regex.Match(Version, @"^(\d+)\.(\d+)\.(\d+)(?:-(alpha|beta|preview|rc)([1-9]))?$");
        if (!match.Success)
        {
            Log.LogError("Launcher version '" + Version + "' must be X.Y.Z or X.Y.Z-(alpha|beta|preview|rc)N, with N from 1 to 9.");
            return false;
        }
        int major = int.Parse(match.Groups[1].Value);
        int minor = int.Parse(match.Groups[2].Value);
        int patch = int.Parse(match.Groups[3].Value);
        if (major > 255 || minor > 255 || patch > 654)
        {
            Log.LogError("Launcher version '" + Version + "' doesn't fit in an MSI version (major and minor up to 255, patch up to 654).");
            return false;
        }

        // MSI versions are numbers only: build = patch * 100 + rank, with the rank sorting pre-releases
        // before their release (alpha 11-19, beta 31-39, preview 51-59, rc 71-79, release 99)
        int rank = 99;
        switch (match.Groups[4].Value)
        {
            case "alpha": rank = 10; break;
            case "beta": rank = 30; break;
            case "preview": rank = 50; break;
            case "rc": rank = 70; break;
        }
        if (rank != 99)
            rank += int.Parse(match.Groups[5].Value);
        MsiVersion = major + "." + minor + "." + (patch * 100 + rank);

        // Name-based UUID (RFC 4122 version 5) of the version: a new ProductCode for each version
        // (MSI major upgrade), the same one when a version is built again
        var namespaceBytes = new Guid("A74B936D-BB1A-4633-A140-A51FCFD372EA").ToByteArray();
        Array.Reverse(namespaceBytes, 0, 4);
        Array.Reverse(namespaceBytes, 4, 2);
        Array.Reverse(namespaceBytes, 6, 2);
        var nameBytes = Encoding.UTF8.GetBytes("Stride.Launcher.Setup/" + Version);
        var data = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, data, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, data, namespaceBytes.Length, nameBytes.Length);
        byte[] hash;
        using (var sha1 = SHA1.Create())
            hash = sha1.ComputeHash(data);
        var uuid = new byte[16];
        Array.Copy(hash, uuid, 16);
        uuid[6] = (byte)((uuid[6] & 0x0F) | 0x50);
        uuid[8] = (byte)((uuid[8] & 0x3F) | 0x80);
        Array.Reverse(uuid, 0, 4);
        Array.Reverse(uuid, 4, 2);
        Array.Reverse(uuid, 6, 2);
        ProductCode = new Guid(uuid).ToString("B").ToUpperInvariant();
        return true;
    }
}
