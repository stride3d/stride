// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Xunit;
using Stride.Core.Windows;

namespace Stride.Core.Design.Tests
{
    public class TestFileLock
    {
        [Fact]
        public void TestFilelockWait()
        {
            FileLock mutex;
            bool flag;

            // Creating lock in current directory.
            using (mutex = FileLock.Wait("something.lock"))
            {
            }

            // Explicitely creating lock in current directory.
            var dir = Directory.GetCurrentDirectory();
            using (mutex = FileLock.Wait(Path.Combine(dir, "something.lock")))
            {
            }

            // Creating lock in a directory that does not yet exist, it should throw an exception.
            flag = false;
            var guidDir = Path.Combine(dir, Guid.NewGuid().ToString());
            try
            {
                using (mutex = FileLock.Wait(Path.Combine(guidDir, "something.lock")))
                {
                    // This should never happen. So throw an exception and make sure it is not caught by our catch below.
                    flag = true;
                    Assert.True(false, "Cannot create a file lock if parent directory does not exist.");
                }
            }
            catch (Exception)
            {
                if (flag) throw;
            }

            // Create lock in a directory that exists.
            Directory.CreateDirectory(guidDir);
            using (mutex = FileLock.Wait(Path.Combine(dir, guidDir, "something.lock")))
            {
            }

            // Delete our temporary directory.
            Directory.Delete(guidDir, true);
        }
    }
}
