// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if !XENKO_RUNTIME_CORECLR && !XENKO_PLATFORM_UWP
using System;
using System.Collections.Generic;

namespace Xenko
{
    class ProcessOutputs
    {
        public int ExitCode { get; set; }

        public List<string> OutputLines { get; private set; }

        public List<string> OutputErrors { get; private set; }

        public string OutputAsString { get { return string.Join(Environment.NewLine, OutputLines); } }

        public string ErrorsAsString { get { return string.Join(Environment.NewLine, OutputErrors); } }

        public ProcessOutputs()
        {
            OutputLines = new List<string>();
            OutputErrors = new List<string>();
        }
    }
}
#endif
