// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_UWP
using System;

// Some missing delegates/exceptions when compiling against WinRT/WinPhone 8.1
namespace Xenko.Core
{
    public delegate void ThreadStart();

    public delegate void ParameterizedThreadStart(object obj);

    class ThreadAbortException : Exception
    {
        
    }
}
#endif
