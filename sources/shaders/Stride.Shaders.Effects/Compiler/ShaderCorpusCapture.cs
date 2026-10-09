// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Stride.Shaders.Compiler
{
    /// <summary>
    /// Records every mixin tree given to an effect compiler as a <see cref="ShaderCorpusEntry"/> file, when the
    /// <c>STRIDE_SHADER_CORPUS_CAPTURE</c> environment variable names a folder. <c>STRIDE_SHADER_CORPUS_TAG</c> is stored as the
    /// entry source (the entry assembly name if unset, e.g. the game or the asset compiler). Used to refresh the shader corpus from a running game or an asset build.
    /// </summary>
    public static class ShaderCorpusCapture
    {
        private static readonly string? CaptureDirectory = Environment.GetEnvironmentVariable("STRIDE_SHADER_CORPUS_CAPTURE");

        public static void Record(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters)
        {
            if (string.IsNullOrEmpty(CaptureDirectory) || string.IsNullOrEmpty(mixinTree.Name))
                return;

            try
            {
                // The entry assembly names tools run by the dotnet host (e.g. the asset compiler), which all share one process name
                var tag = Environment.GetEnvironmentVariable("STRIDE_SHADER_CORPUS_TAG")
                    ?? Assembly.GetEntryAssembly()?.GetName().Name
                    ?? Process.GetCurrentProcess().ProcessName;
                var entry = new ShaderCorpusEntry(mixinTree.Name, effectParameters.Profile, mixinTree);
                entry.Sources.Add(tag);

                // One file per permutation and tag: processes never write the same file, and merging the folder gives each
                // permutation every source that compiled it, whatever the order
                Directory.CreateDirectory(CaptureDirectory);
                var safeTag = string.Concat(Array.ConvertAll(tag.ToCharArray(), c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));
                var path = Path.Combine(CaptureDirectory, $"{entry.Id}.{safeTag}.json");
                if (File.Exists(path))
                    return;

                // Write then move, so a reader (or another compiler thread) never sees a partial file
                var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
                File.WriteAllText(tempPath, ShaderCorpusEntry.ToJson(entry));
                try
                {
                    File.Move(tempPath, path);
                }
                catch (IOException)
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception e)
            {
                // Capture is a debug aid, it must never fail a compile
                Debug.WriteLine($"Shader corpus capture failed for {mixinTree.Name}: {e.Message}");
            }
        }
    }
}
