// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_UWP
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Xenko.Core.IO
{
    public class NativeFile
    {
        private const string KERNEL_FILE2 = "api-ms-win-core-file-l2-1-1.dll";
        private const string KERNEL_FILE = "api-ms-win-core-file-l1-2-1.dll";

        [DllImport(KERNEL_FILE, EntryPoint = "GetFileAttributesExW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetFileAttributesEx(string name, int fileInfoLevel, out WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

        [DllImport(KERNEL_FILE, EntryPoint = "DeleteFileW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteFile(string name);

        [DllImport(KERNEL_FILE, EntryPoint = "CreateDirectoryW", CharSet = CharSet.Unicode, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateDirectory(string lpPathName, IntPtr lpSecurityAttributes);

        private const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        internal struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint FileSizeHigh;
            public uint FileSizeLow;
        }

        [DllImport(KERNEL_FILE2, EntryPoint = "MoveFileExW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        static extern bool MoveFileEx(String src, String dst, uint flags);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FileDelete(string name)
        {
            if (!DeleteFile(name))
            {
                // TODO: Process GetLastError() code.
                throw new IOException(string.Format("Error deleting file {0}", name));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FileMove(string sourceFileName, string destFileName)
        {
            if (!MoveFileEx(sourceFileName, destFileName, 2 /* MOVEFILE_COPY_ALLOWED */))
            {
                // TODO: Process GetLastError() code.
                throw new IOException(string.Format("Can't move file {0} to {1}", sourceFileName, destFileName));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long FileSize(string name)
        {
            WIN32_FILE_ATTRIBUTE_DATA data;
            if (!GetFileAttributesEx(name, 0, out data))
                throw new FileNotFoundException("File not found.", name);

            return ((long)data.FileSizeHigh << 32) & data.FileSizeLow;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FileExists(string name)
        {
            try
            {
                WIN32_FILE_ATTRIBUTE_DATA win_file_attribute_data;
                if (GetFileAttributesEx(name, 0, out win_file_attribute_data))
                {
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DirectoryExists(string name)
        {
            WIN32_FILE_ATTRIBUTE_DATA win_file_attribute_data;
            if (!GetFileAttributesEx(name, 0, out win_file_attribute_data))
                return false;

            return (win_file_attribute_data.FileAttributes != INVALID_FILE_ATTRIBUTES &&
                (win_file_attribute_data.FileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DirectoryCreate(string path)
        {
            CreateDirectory(path, IntPtr.Zero);
        }
    }
}
#endif
