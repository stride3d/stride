// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Tests.IO
{
    public class DatabaseFileProviderTests
    {
        [Theory]
        [InlineData("Root/A", "Root", "A", VirtualSearchOption.TopDirectoryOnly)]
        [InlineData("Root/A", "Root", "A", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/A", "Root/", "A", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/A", "Root", "*", VirtualSearchOption.TopDirectoryOnly)]
        [InlineData("Root/A", "Root", "*", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/A", "Root/", "*", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/Dir/A", "Root", "A", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/Dir/A", "Root", "*", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/A", "Root", "?", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/Abc", "Root", "A?c", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/Abbc", "Root", "A*c", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/Abbc", "Root", "A*", VirtualSearchOption.AllDirectories)]
        public void ListFilesRegex_Matches(string url, string pathPrefix, string fileNamePattern, VirtualSearchOption options)
        {
            var regex = DatabaseFileProvider.CreateRegexForFileSearch(pathPrefix, fileNamePattern, options);
            Assert.Matches(regex, url);
        }

        [Theory]
        [InlineData("Root/A", "Root", "B", VirtualSearchOption.TopDirectoryOnly)]
        [InlineData("Root/A", "Root", "B", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/Dir/A", "Root", "B", VirtualSearchOption.AllDirectories)]
        [InlineData("Root/Dir/A", "Root", "*", VirtualSearchOption.TopDirectoryOnly)]
        [InlineData("Root/Abbc", "Root", "A?c", VirtualSearchOption.AllDirectories)]
        // if path starts with / it won't match because URLs in DatabaseFileProvider don't have a preceding /
        [InlineData("Root/A", "/Root", "A", VirtualSearchOption.AllDirectories)]
        public void ListFilesRegex_DoesNotMatch(string url, string pathPrefix, string fileNamePattern, VirtualSearchOption options)
        {
            var regex = DatabaseFileProvider.CreateRegexForFileSearch(pathPrefix, fileNamePattern, options);
            Assert.DoesNotMatch(regex, url);
        }
    }
}
