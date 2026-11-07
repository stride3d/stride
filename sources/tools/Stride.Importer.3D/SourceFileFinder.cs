// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Assimp;
using AssimpFile = Silk.NET.Assimp.File;

namespace Stride.Importer.ThreeD
{
    /// <summary>
    /// Finds the files a model reads besides itself, by reading the model with a file system that only opens files for reading
    /// and records them, and by resolving the texture references of its materials.
    /// </summary>
    internal static unsafe class SourceFileFinder
    {
        public static List<(string Source, string RelativePath)> Find(Assimp assimp, string modelPath)
        {
            modelPath = Path.GetFullPath(modelPath);
            var modelDirectory = Path.GetDirectoryName(modelPath)!;
            var files = new List<(string Source, string RelativePath)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { modelPath };

            var recorder = new Recorder();
            var recorderHandle = GCHandle.Alloc(recorder);
            try
            {
                var fileIO = new FileIO
                {
                    OpenProc = new PfnFileOpenProc(&Open),
                    CloseProc = new PfnFileCloseProc(&Close),
                    UserData = (byte*)GCHandle.ToIntPtr(recorderHandle),
                };
                var scene = assimp.ImportFileEx(modelPath, 0, &fileIO);
                if (scene == null)
                    return files;

                try
                {
                    // Files the importer itself opens (a glTF buffer, an OBJ material library) are read by their path relative to the model.
                    foreach (var opened in recorder.OpenedFiles)
                    {
                        if (seen.Add(opened))
                            files.Add((opened, Path.GetRelativePath(modelDirectory, opened)));
                    }

                    // Textures are read next to the model, by file name, wherever the model references them.
                    foreach (var reference in GetTextureReferences(assimp, scene))
                    {
                        var source = FindTexture(modelDirectory, reference);
                        if (source != null && seen.Add(source))
                            files.Add((source, Path.GetFileName(source)));
                    }
                }
                finally
                {
                    assimp.ReleaseImport(scene);
                }
            }
            finally
            {
                recorderHandle.Free();
            }

            return files;
        }

        private static IEnumerable<string> GetTextureReferences(Assimp assimp, Scene* scene)
        {
            var references = new List<string>();
            for (uint i = 0; i < scene->MNumMaterials; i++)
            {
                var material = scene->MMaterials[i];
                foreach (var type in Enum.GetValues<TextureType>())
                {
                    if (type == TextureType.None)
                        continue;

                    var count = assimp.GetMaterialTextureCount(material, type);
                    for (uint j = 0; j < count; j++)
                    {
                        var path = new AssimpString();
                        if (assimp.GetMaterialString(material, Assimp.MaterialTextureBase, (uint)type, j, ref path) != Return.Success)
                            continue;

                        var reference = path.AsString;
                        // Embedded textures are referenced as '*' followed by their index; they are part of the model.
                        if (!string.IsNullOrEmpty(reference) && !reference.StartsWith(Assimp.EmbeddedTexnamePrefix, StringComparison.Ordinal))
                            references.Add(reference);
                    }
                }
            }
            return references;
        }

        private static string FindTexture(string modelDirectory, string reference)
        {
            var normalized = reference.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

            // The file next to the model comes first, because that is the one the importer reads.
            var nextToModel = Path.Combine(modelDirectory, Path.GetFileName(normalized));
            if (System.IO.File.Exists(nextToModel))
                return nextToModel;

            string asReferenced = null;
            try
            {
                asReferenced = Path.GetFullPath(Path.Combine(modelDirectory, normalized));
            }
            catch (ArgumentException)
            {
            }

            return asReferenced != null && System.IO.File.Exists(asReferenced) ? asReferenced : null;
        }

        private sealed class Recorder
        {
            public readonly List<string> OpenedFiles = new();
        }

        // An exception that leaves an UnmanagedCallersOnly method ends the process, so the callbacks report failures like the C file API does.
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static AssimpFile* Open(FileIO* fileIO, byte* path, byte* mode)
        {
            var modeText = Marshal.PtrToStringUTF8((IntPtr)mode) ?? "rb";
            if (modeText.Contains('w') || modeText.Contains('a') || modeText.Contains('+'))
                return null;

            string fullPath;
            FileStream stream;
            try
            {
                fullPath = Path.GetFullPath(Marshal.PtrToStringUTF8((IntPtr)path)!);
                stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception)
            {
                return null;
            }

            var recorder = (Recorder)GCHandle.FromIntPtr((IntPtr)fileIO->UserData).Target!;
            recorder.OpenedFiles.Add(fullPath);

            var file = (AssimpFile*)NativeMemory.AllocZeroed((nuint)sizeof(AssimpFile));
            file->ReadProc = new PfnFileReadProc(&Read);
            file->WriteProc = new PfnFileReadProc(&Write);
            file->TellProc = new PfnFileTellProc(&Tell);
            file->FileSizeProc = new PfnFileTellProc(&Size);
            file->SeekProc = new PfnFileSeek(&Seek);
            file->FlushProc = new PfnFileFlushProc(&Flush);
            file->UserData = (byte*)GCHandle.ToIntPtr(GCHandle.Alloc(stream));
            return file;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Close(FileIO* fileIO, AssimpFile* file)
        {
            var handle = GCHandle.FromIntPtr((IntPtr)file->UserData);
            ((FileStream)handle.Target!).Dispose();
            handle.Free();
            NativeMemory.Free(file);
        }

        private static FileStream GetStream(AssimpFile* file) => (FileStream)GCHandle.FromIntPtr((IntPtr)file->UserData).Target!;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static nuint Read(AssimpFile* file, byte* buffer, nuint size, nuint count)
        {
            if (size == 0 || count == 0)
                return 0;

            long read = 0;
            try
            {
                var stream = GetStream(file);
                var total = checked((long)(size * count));
                while (read < total)
                {
                    var chunk = stream.Read(new Span<byte>(buffer + read, (int)Math.Min(total - read, int.MaxValue)));
                    if (chunk == 0)
                        break;
                    read += chunk;
                }
            }
            catch (Exception)
            {
            }
            // Like fread, the result counts whole elements.
            return (nuint)(read / (long)size);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static nuint Write(AssimpFile* file, byte* buffer, nuint size, nuint count) => 0;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static nuint Tell(AssimpFile* file)
        {
            try
            {
                return (nuint)GetStream(file).Position;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static nuint Size(AssimpFile* file)
        {
            try
            {
                return (nuint)GetStream(file).Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Return Seek(AssimpFile* file, nuint offset, Origin origin)
        {
            var stream = GetStream(file);
            var signedOffset = unchecked((long)offset);
            try
            {
                stream.Position = origin switch
                {
                    Origin.Set => signedOffset,
                    Origin.Cur => stream.Position + signedOffset,
                    Origin.End => stream.Length + signedOffset,
                    _ => throw new ArgumentOutOfRangeException(nameof(origin)),
                };
                return Return.Success;
            }
            catch (Exception)
            {
                return Return.Failure;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void Flush(AssimpFile* file)
        {
        }
    }
}
