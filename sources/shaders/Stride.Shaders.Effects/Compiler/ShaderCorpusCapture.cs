// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Stride.Shaders.Compiler
{
    /// <summary>
    /// Records every mixin tree given to an effect compiler as a <see cref="ShaderCorpusEntry"/> file, when the
    /// <c>STRIDE_SHADER_CORPUS_CAPTURE</c> environment variable names a folder. The entry source is
    /// <c>STRIDE_SHADER_CORPUS_TAG</c>, else <see cref="Source"/>, else the entry assembly name (e.g. the game).
    /// </summary>
    public static class ShaderCorpusCapture
    {
        private static readonly string? CaptureDirectory = Environment.GetEnvironmentVariable("STRIDE_SHADER_CORPUS_CAPTURE");
        private static readonly AsyncLocal<bool> NotRecording = new();

        /// <summary>
        /// The entry source when <c>STRIDE_SHADER_CORPUS_TAG</c> is unset, for a process whose entry assembly doesn't name it
        /// (e.g. a test assembly run by the test host).
        /// </summary>
        public static string? Source { get; set; }

        /// <summary>
        /// Runs <paramref name="compile"/> without recording. Used for the replay of an effect log by an asset build: the log
        /// holds what was compiled at some point, not what the current code compiles.
        /// </summary>
        public static T WithoutRecording<T>(Func<T> compile)
        {
            var previous = NotRecording.Value;
            NotRecording.Value = true;
            try
            {
                return compile();
            }
            finally
            {
                NotRecording.Value = previous;
            }
        }

        /// <summary>
        /// Writes <paramref name="mixinTree"/> as a corpus entry file in the capture folder. Does nothing when capture is off,
        /// inside <see cref="WithoutRecording{T}"/>, for a tree without a name, or when this permutation was already written
        /// for the same source. Never throws: a failed write only skips the entry.
        /// </summary>
        /// <param name="mixinTree">The mixin tree given to the effect compiler, before the platform macros are added.</param>
        /// <param name="effectParameters">The parameters of the compile; their profile is part of the entry.</param>
        public static void Record(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters)
        {
            if (string.IsNullOrEmpty(CaptureDirectory) || NotRecording.Value || string.IsNullOrEmpty(mixinTree.Name))
                return;

            try
            {
                // The entry assembly names tools run by the dotnet host (e.g. the asset compiler), which all share one process name
                var tag = Environment.GetEnvironmentVariable("STRIDE_SHADER_CORPUS_TAG")
                    ?? Source
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
