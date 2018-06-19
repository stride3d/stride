// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.IO;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class SessionInitialDirectoryProvider : IInitialDirectoryProvider
    {
        private readonly SessionViewModel session;

        public SessionInitialDirectoryProvider(SessionViewModel session)
        {
            this.session = session;
        }

        public UDirectory GetInitialDirectory(UDirectory currentPath)
        {
            if (session != null && currentPath != null)
            {
                // Take the solution directory by default.
                var sessionPath = session.SolutionPath;
                if (sessionPath == null)
                {
                    // If there is no solution directory, try to use the directory of the first local package.
                    var firstPackage = session.LocalPackages.FirstOrDefault();
                    if (firstPackage != null)
                        sessionPath = firstPackage.PackagePath;
                }

                if (sessionPath != null)
                {
                    var defaultPath = UPath.Combine(sessionPath.GetFullDirectory(), currentPath);
                    return defaultPath.GetFullDirectory();
                }
            }
            return currentPath;
        }
    }
}
