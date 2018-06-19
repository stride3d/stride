// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;

namespace Xenko.ProjectGenerator
{
    public partial class ResharperDotSettings
    {
        public Guid FileInjectedGuid { get; set; }

        public FileInfo SharedSolutionDotSettings { get; set; }
    }
}
