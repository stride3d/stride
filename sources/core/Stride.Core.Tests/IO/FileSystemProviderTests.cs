using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Tests.IO;

public class FileSystemProviderTests
{
    [Fact]
    public void Constructor_SetsRootPath()
    {
        // Use unique root path for each test to avoid conflicts
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, null);

        try
        {
            // RootPath adds a trailing slash
            Assert.Equal(rootPath + "/", provider.RootPath);
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void GetAbsolutePath_CombinesWithBasePath()
    {
        var tempDir = Path.GetTempPath();
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, tempDir);

        try
        {
            var absolutePath = provider.GetAbsolutePath("file.txt");

            Assert.Contains(tempDir, absolutePath);
            Assert.EndsWith("file.txt", absolutePath);
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void DirectoryExists_ReturnsFalseForNonExistentDirectory()
    {
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, Path.GetTempPath());

        try
        {
            var exists = provider.DirectoryExists(Guid.NewGuid().ToString());

            Assert.False(exists);
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void CreateDirectory_CreatesDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "stride_test_" + Guid.NewGuid().ToString());
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, tempDir);

        try
        {
            provider.CreateDirectory("testdir");

            Assert.True(provider.DirectoryExists("testdir"));
        }
        finally
        {
            provider.Dispose();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FileExists_ReturnsFalseForNonExistentFile()
    {
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, Path.GetTempPath());

        try
        {
            var exists = provider.FileExists(Guid.NewGuid().ToString() + ".txt");

            Assert.False(exists);
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void OpenStream_CreateNew_CreatesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "stride_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, tempDir);
        var fileName = "testfile.txt";

        try
        {
            using (var stream = provider.OpenStream(fileName, VirtualFileMode.CreateNew, VirtualFileAccess.Write))
            {
                Assert.NotNull(stream);
                Assert.True(stream.CanWrite);
            }

            Assert.True(provider.FileExists(fileName));
        }
        finally
        {
            provider.Dispose();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void OpenStream_Read_ReadsFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "stride_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, tempDir);
        var fileName = "testfile.txt";
        var testData = "Hello, World!";

        try
        {
            // Write file
            using (var stream = provider.OpenStream(fileName, VirtualFileMode.Create, VirtualFileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(testData);
            }

            // Read file
            using (var stream = provider.OpenStream(fileName, VirtualFileMode.Open, VirtualFileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();
                Assert.Equal(testData, content);
            }
        }
        finally
        {
            provider.Dispose();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FileDelete_DeletesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "stride_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, tempDir);
        var fileName = "testfile.txt";

        try
        {
            using (var stream = provider.OpenStream(fileName, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                // Just create the file
            }

            Assert.True(provider.FileExists(fileName));

            provider.FileDelete(fileName);

            Assert.False(provider.FileExists(fileName));
        }
        finally
        {
            provider.Dispose();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FileMove_MovesFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "stride_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, tempDir);
        var sourceFile = "source.txt";
        var destFile = "dest.txt";

        try
        {
            using (var stream = provider.OpenStream(sourceFile, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                // Just create the file
            }

            Assert.True(provider.FileExists(sourceFile));

            provider.FileMove(sourceFile, destFile);

            Assert.False(provider.FileExists(sourceFile));
            Assert.True(provider.FileExists(destFile));
        }
        finally
        {
            provider.Dispose();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FileSize_ReturnsCorrectSize()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "stride_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, tempDir);
        var fileName = "testfile.txt";
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        try
        {
            using (var stream = provider.OpenStream(fileName, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                stream.Write(testData, 0, testData.Length);
            }

            var size = provider.FileSize(fileName);

            Assert.Equal(testData.Length, size);
        }
        finally
        {
            provider.Dispose();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ListFiles_ReturnsMatchingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "stride_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var rootPath = "/test_" + Guid.NewGuid().ToString("N")[..8];
        var provider = new FileSystemProvider(rootPath, tempDir);

        try
        {
            using (provider.OpenStream("file1.txt", VirtualFileMode.Create, VirtualFileAccess.Write)) { }
            using (provider.OpenStream("file2.txt", VirtualFileMode.Create, VirtualFileAccess.Write)) { }
            using (provider.OpenStream("file3.dat", VirtualFileMode.Create, VirtualFileAccess.Write)) { }

            var txtFiles = provider.ListFiles("", "*.txt", VirtualSearchOption.TopDirectoryOnly);

            Assert.Equal(2, txtFiles.Length);
            Assert.Contains(txtFiles, f => f.EndsWith("file1.txt"));
            Assert.Contains(txtFiles, f => f.EndsWith("file2.txt"));
        }
        finally
        {
            provider.Dispose();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
