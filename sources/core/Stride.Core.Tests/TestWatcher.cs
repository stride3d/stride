// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_WINDOWS_DESKTOP
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using Stride.Core.IO;

namespace Stride.Core.Tests
{
    public class TestWatcher
    {
        [Fact(Skip = "Need check")]
        public void TestDirectory()
        {
            var tempDirectory = new DirectoryInfo("Temp." + typeof(TestWatcher).Assembly.GetName().Name);

            RemoveDirectory(tempDirectory);
            if (!tempDirectory.Exists)
            {
                tempDirectory.Create();
            }

            var pa0 = GetDirectoryPath(tempDirectory, @"a0");
            var pb0 = GetDirectoryPath(tempDirectory, @"a0\b0");
            var p1 = CreateDirectoryPath(tempDirectory, @"a0\b0\c0");
            var p2 = CreateDirectoryPath(tempDirectory, @"a0\b0\c1");
            var p3 = CreateDirectoryPath(tempDirectory, @"a0\b0\c2");

            var watcher = new DirectoryWatcher();
            watcher.Track(p1);
            var list = watcher.GetTrackedDirectories();
            Assert.Single(list);
            Assert.Equal(p1, list[0]);

            watcher.Track(p2);
            list = watcher.GetTrackedDirectories();
            Assert.Equal(2, list.Count);
            Assert.Equal(p1, list[0]);
            Assert.Equal(p2, list[1]);

            // Adding p3 should set the track on the parent directory
            watcher.Track(p3);
            list = watcher.GetTrackedDirectories();
            Assert.Single(list);
            Assert.Equal(pb0, list[0]);

            // Tracking again a child should not add a new track as the parent is already tracking
            watcher.Track(p1);
            list = watcher.GetTrackedDirectories();
            Assert.Single(list);
            Assert.Equal(pb0, list[0]);

            watcher.Track(pb0);
            list = watcher.GetTrackedDirectories();
            Assert.Single(list);
            Assert.Equal(pb0, list[0]);

            var events = new List<FileEvent>();
            EventHandler<FileEvent> fileEventHandler = (sender, args) => events.Add(args);

            watcher.Modified += fileEventHandler;
            var p4 = CreateDirectoryPath(tempDirectory, @"a0\b0\c3");
            Thread.Sleep(20);
            watcher.Modified -= fileEventHandler;

            Assert.Single(events);
            Assert.Equal(p4, events[0].FullPath.ToLowerInvariant());

            events.Clear();
            watcher.Modified += fileEventHandler;
            RemoveDirectory(new DirectoryInfo(pb0));
            Thread.Sleep(400);
            watcher.Modified -= fileEventHandler;

            Assert.True(events.All(args => args.ChangeType == FileEventChangeType.Deleted)); // c0, c1, c2, c3 removed

            //// We should not track any directory
            //list = watcher.GetTrackedDirectories();
            //Assert.Equal(0, list.Count);

            RemoveDirectory(tempDirectory);
        }

        private string GetDirectoryPath(DirectoryInfo root, string subPath)
        {
            var tempDirectory = Path.Combine(root.FullName, subPath);
            return tempDirectory.ToLowerInvariant();
        }

        private string CreateDirectoryPath(DirectoryInfo root, string subPath)
        {
            var tempDirectory = GetDirectoryPath(root, subPath);
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }
            return tempDirectory;
        }

        private void RemoveDirectory(DirectoryInfo path)
        {
            if (Directory.Exists(path.FullName))
            {
                TemporaryDirectory.DeleteDirectory(path.FullName);
            }
            if (Directory.Exists(path.FullName))
            {
                Trace.WriteLine(string.Format("Unable to remove directory {0}", path.FullName));
            }
        }
    }
}
#endif
