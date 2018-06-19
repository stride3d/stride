// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_UWP || XENKO_RUNTIME_CORECLR
namespace System.ComponentModel
{
    public class BrowsableAttribute : Attribute
    {
        public BrowsableAttribute(bool browsable)
        {
            Browsable = browsable;
        }

        public bool Browsable { get; private set; }
    }
}
#endif
