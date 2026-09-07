// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace Stride.CrashReport
{
    /// <summary>
    /// Minimal post-mortem reader for the two minidump streams a native-crash signature needs: the exception
    /// record (the faulting address) and the module list (to turn that address into <c>module+0x&lt;rva&gt;</c>).
    /// Used to derive a signature from a dump written by the runtime's <c>createdump</c> on Linux/macOS, where —
    /// unlike the Windows vectored handler — nothing computes it at fault time. The dump is a Windows-format
    /// minidump on every platform (that is what <c>createdump --minidump</c> writes), so the layout is the same.
    /// </summary>
    internal static class MinidumpReader
    {
        private const uint MinidumpSignature = 0x504D444D; // 'MDMP'
        private const uint ModuleListStream = 4;
        private const uint ExceptionStream = 6;
        private const int DirectoryEntrySize = 12; // StreamType(4) + DataSize(4) + Rva(4)
        private const int ModuleRecordSize = 108;  // sizeof(MINIDUMP_MODULE) on x64

        /// <summary>
        /// Returns the faulting frame as <c>&lt;module&gt;+0x&lt;rva&gt;</c>, or null when the dump has no exception
        /// stream, the address lands outside every module (JIT'd managed code), or the file can't be parsed. Never throws.
        /// </summary>
        public static string FaultingFrame(string dumpPath)
        {
            try
            {
                return FaultingFrame(File.ReadAllBytes(dumpPath));
            }
            catch
            {
                return null;
            }
        }

        public static string FaultingFrame(byte[] dump)
        {
            if (dump.Length < 16 || ReadU32(dump, 0) != MinidumpSignature)
                return null;

            var streamCount = ReadU32(dump, 8);
            var directoryRva = ReadU32(dump, 12);

            ulong faultingAddress = 0;
            long moduleListRva = -1;
            for (uint i = 0; i < streamCount; i++)
            {
                long entry = directoryRva + (long)i * DirectoryEntrySize;
                if (entry + DirectoryEntrySize > dump.Length)
                    break;
                var streamType = ReadU32(dump, entry);
                var dataRva = ReadU32(dump, entry + 8);
                if (streamType == ExceptionStream)
                    faultingAddress = ReadU64(dump, dataRva + 24); // MINIDUMP_EXCEPTION.ExceptionAddress
                else if (streamType == ModuleListStream)
                    moduleListRva = dataRva;
            }

            if (faultingAddress == 0 || moduleListRva < 0)
                return null;

            var moduleCount = ReadU32(dump, moduleListRva);
            for (uint i = 0; i < moduleCount; i++)
            {
                long module = moduleListRva + 4 + (long)i * ModuleRecordSize;
                if (module + ModuleRecordSize > dump.Length)
                    break;
                var imageBase = ReadU64(dump, module);
                var imageSize = ReadU32(dump, module + 8);
                if (faultingAddress < imageBase || faultingAddress >= imageBase + imageSize)
                    continue;

                var name = ReadMinidumpString(dump, ReadU32(dump, module + 20)); // MINIDUMP_MODULE.ModuleNameRva
                var baseName = string.IsNullOrEmpty(name) ? "module" : Path.GetFileName(name);
                return $"{baseName}+0x{faultingAddress - imageBase:x}";
            }
            return null;
        }

        /// <summary>
        /// The OS id of the faulting thread, from the dump's exception stream, or null when the dump has none.
        /// Present in <c>createdump</c> dumps on Linux/macOS (and Windows once dotnet/runtime#133065 ships), so the
        /// dump walk can pick the crashing thread exactly there; absent on Windows <c>createdump</c> today.
        /// </summary>
        public static uint? FaultingThreadId(string dumpPath)
        {
            try
            {
                return FaultingThreadId(File.ReadAllBytes(dumpPath));
            }
            catch
            {
                return null;
            }
        }

        public static uint? FaultingThreadId(byte[] dump)
        {
            if (dump.Length < 16 || ReadU32(dump, 0) != MinidumpSignature)
                return null;

            var streamCount = ReadU32(dump, 8);
            var directoryRva = ReadU32(dump, 12);
            for (uint i = 0; i < streamCount; i++)
            {
                long entry = directoryRva + (long)i * DirectoryEntrySize;
                if (entry + DirectoryEntrySize > dump.Length)
                    break;
                if (ReadU32(dump, entry) != ExceptionStream)
                    continue;
                var dataRva = ReadU32(dump, entry + 8);
                if (dataRva + 4 <= dump.Length)
                    return ReadU32(dump, dataRva); // MINIDUMP_EXCEPTION_STREAM.ThreadId
            }
            return null;
        }

        // MINIDUMP_STRING: a uint byte-length (not char count) followed by UTF-16LE text.
        private static string ReadMinidumpString(byte[] dump, uint rva)
        {
            if (rva + 4 > dump.Length)
                return null;
            var byteLength = ReadU32(dump, rva);
            long start = rva + 4;
            if (start + byteLength > dump.Length)
                byteLength = (uint)Math.Max(0, dump.Length - start);
            return Encoding.Unicode.GetString(dump, (int)start, (int)byteLength);
        }

        private static uint ReadU32(byte[] b, long o) => (uint)(b[o] | b[o + 1] << 8 | b[o + 2] << 16 | b[o + 3] << 24);
        private static ulong ReadU64(byte[] b, long o) => ReadU32(b, o) | ((ulong)ReadU32(b, o + 4) << 32);
    }
}
