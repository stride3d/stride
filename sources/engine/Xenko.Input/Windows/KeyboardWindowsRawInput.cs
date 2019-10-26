// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_INPUT_RAWINPUT
using System;

namespace Xenko.Input
{
    internal class KeyboardWindowsRawInput : KeyboardDeviceBase
    {
        public KeyboardWindowsRawInput(InputSourceWindowsRawInput source)
        {
            // Raw input is usually preferred above other keyboards
            Priority = 100;
            Source = source;
        }

        public override string Name => "Windows Keyboard (Raw Input)";

        public override Guid Id => new Guid("d7437ff5-d14f-4491-9673-377b6d0e241c");

        public override IInputSource Source { get; }
    }
}
#endif