// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Dev-packages staleness check, compiled at runtime by RoslynCodeTaskFactory (no prebuilt task assembly).
// Referenced from Stride.DevPackages.targets via <Code Source>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// Two questions about one project's stub, answered from the generator's files (see build/GenerateDevPackages.cs):
//  - Changed: the inputs of the manifest ("<sha256>\t<path>" per line: csprojs and packed build/ assets) whose
//    content changed since the stubs were generated. A file older than the stamp is trusted without being read; a
//    newer one is confirmed by content hash, so mtime churn alone (touch, checkout, rebase) is not a change. A
//    missing file counts as changed. The targets keep the ones under the project's own directory.
//  - OwnStubFresh: the stubs file lists, per stub, the fingerprint of the fixed inputs (the script, the central
//    package versions, the solution) it was generated with; the project's stub is fresh when its line carries the
//    fingerprint of those files as they are now. Every stub depends on the fixed inputs, so each project judges
//    its own stub instead of a shared "everything is stale" that the first refresh would hide from the others.
public class StrideCheckDevPackageInputs : Task
{
    [Required] public string ManifestPath { get; set; }
    [Required] public string StampPath { get; set; }
    [Required] public string StubsPath { get; set; }
    [Required] public ITaskItem[] FixedInputs { get; set; }
    [Required] public string OwnStub { get; set; }
    [Output] public ITaskItem[] Changed { get; set; }
    [Output] public bool OwnStubFresh { get; set; }

    public override bool Execute()
    {
        var changed = new List<ITaskItem>();
        var stampTime = File.GetLastWriteTimeUtc(StampPath);
        foreach (var rawLine in File.Exists(ManifestPath) ? File.ReadAllLines(ManifestPath) : new string[0])
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;
            var tab = line.IndexOf('\t');
            var expectedHash = tab >= 0 ? line.Substring(0, tab) : null;
            var path = tab >= 0 ? line.Substring(tab + 1) : line;
            if (!File.Exists(path))
            {
                changed.Add(new TaskItem(path));
                continue;
            }
            if (File.GetLastWriteTimeUtc(path) <= stampTime)
                continue; // untouched since generation: trusted without reading
            if (expectedHash == null || !string.Equals(ContentHash(path), expectedHash, StringComparison.OrdinalIgnoreCase))
                changed.Add(new TaskItem(path));
        }
        Changed = changed.ToArray();

        // Fresh = the stub file is in the feed (next to the stamp) and its recorded fingerprint is the current one.
        var fingerprint = Fingerprint(FixedInputs);
        OwnStubFresh = false;
        if (File.Exists(Path.Combine(Path.GetDirectoryName(StampPath), OwnStub)))
        {
            foreach (var rawLine in File.Exists(StubsPath) ? File.ReadAllLines(StubsPath) : new string[0])
            {
                var tab = rawLine.IndexOf('\t');
                if (tab < 0) continue;
                if (string.Equals(rawLine.Substring(0, tab), OwnStub, StringComparison.OrdinalIgnoreCase))
                    OwnStubFresh = string.Equals(rawLine.Substring(tab + 1).Trim(), fingerprint, StringComparison.OrdinalIgnoreCase);
            }
        }
        return true;
    }

    // Same as the generator's Fingerprint: the content hashes of the fixed inputs, joined by '\n', hashed again.
    private static string Fingerprint(ITaskItem[] files)
    {
        var hashes = new List<string>();
        foreach (var file in files)
            hashes.Add(File.Exists(file.ItemSpec) ? ContentHash(file.ItemSpec) : "missing");
        return HexSha256(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", hashes)));
    }

    // Same normalization as the generator's ContentHash: CRLF folded to LF, SHA-256, lowercase hex.
    private static string ContentHash(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var normalized = new byte[bytes.Length];
        var length = 0;
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == (byte)'\r' && i + 1 < bytes.Length && bytes[i + 1] == (byte)'\n')
                continue;
            normalized[length++] = bytes[i];
        }
        var trimmed = new byte[length];
        Array.Copy(normalized, trimmed, length);
        return HexSha256(trimmed);
    }

    private static string HexSha256(byte[] data)
    {
        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(data);
            var hex = new char[hash.Length * 2];
            for (var i = 0; i < hash.Length; i++)
            {
                hex[i * 2] = "0123456789abcdef"[hash[i] >> 4];
                hex[i * 2 + 1] = "0123456789abcdef"[hash[i] & 0xF];
            }
            return new string(hex);
        }
    }
}
