// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Tests.IO;

public class DriveFileProviderTests
{
    [Fact]
    public void Constructor_CreatesProvider()
    {
        var rootPath = "/drive_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new DriveFileProvider(rootPath);

        try
        {
            Assert.NotNull(provider);
            Assert.Equal(rootPath + "/", provider.RootPath);
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void DefaultRootPath_IsSetCorrectly()
    {
        Assert.Equal("/drive", DriveFileProvider.DefaultRootPath);
    }

    [Fact]
    public void GetLocalPath_ConvertsFilePath_OnWindows()
    {
        // Skip test on non-Windows platforms
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var rootPath = "/drive_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new DriveFileProvider(rootPath);

        try
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var localPath = provider.GetLocalPath(tempFile);

                // GetLocalPath returns paths like "/C/Users/..." on Windows
                Assert.NotNull(localPath);
                Assert.StartsWith("/", localPath);
                Assert.Contains("/", localPath);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void FileOperations_WorkWithDriveProvider()
    {
        var rootPath = "/drive_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new DriveFileProvider(rootPath);

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "stride_test_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);

            try
            {
                var tempFile = Path.Combine(tempDir, "test.txt");
                File.WriteAllText(tempFile, "test content");

                var vfsPath = provider.GetLocalPath(tempFile);
                var exists = provider.FileExists(vfsPath);

                Assert.True(exists);

                var size = provider.FileSize(vfsPath);
                Assert.True(size > 0);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void OpenStream_ReadsFileFromDrive()
    {
        var rootPath = "/drive_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new DriveFileProvider(rootPath);

        try
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var content = "Hello, DriveFileProvider!";
                File.WriteAllText(tempFile, content);

                var vfsPath = provider.GetLocalPath(tempFile);
                using var stream = provider.OpenStream(vfsPath, VirtualFileMode.Open, VirtualFileAccess.Read);
                using var reader = new StreamReader(stream);
                var readContent = reader.ReadToEnd();

                Assert.Equal(content, readContent);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
        finally
        {
            provider.Dispose();
        }
    }
}
