// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
namespace Stride.Input.RawInput
{
    internal enum RawInputDataType
    {
        RID_HEADER = 0x10000005,
        RID_INPUT = 0x10000003
    }
}
#endif
