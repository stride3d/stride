// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace Stride.Games
{
    /// <summary>
    /// Parameters used when launching an application.
    /// </summary>
    public class LaunchParameters : Dictionary<string, string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LaunchParameters" /> class.
        /// </summary>
        public LaunchParameters()
        {
#if STRIDE_PLATFORM_WINDOWS_DESKTOP
#if !STRIDE_RUNTIME_CORECLR
            var args = Environment.GetCommandLineArgs();
#else
                // FIXME: Manu: Currently we cannot get the command line arguments in CoreCLR.
            string[] args = new string [] { };
#endif

            if (args.Length > 1)
            {
                var trimChars = new[] { '/', '-' };

                for (int i = 1; i < args.Length; i++)
                {
                    var argument = args[i].TrimStart(trimChars);
                    string key;
                    var value = string.Empty;

                    int index = argument.IndexOf(':');
                    if (index != -1)
                    {
                        key = argument.Substring(0, index);
                        value = argument.Substring(index + 1);
                    }
                    else
                    {
                        key = argument;
                    }

                    if (!ContainsKey(key) && (key != string.Empty))
                    {
                        Add(key, value);
                    }
                }
            }
#endif
        }
    }
}
