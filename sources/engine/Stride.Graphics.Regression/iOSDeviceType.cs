// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
using System;
using System.Runtime.InteropServices;
using ObjCRuntime;

namespace Stride.Graphics.Regression
{
    public class iOSDeviceType
    {
        public enum HardwareModel
        {
            iPhone1G,
            iPhone3G,
            iPhone3GS,
            iPhone4,
            iPhone4S,
            iPhone5,
            iPhone5S,
            iPod1G,
            iPod2G,
            iPod3G,
            iPod4G,
            iPod5G,
            iPad,
            iPad2,
            iPad3,
            iPad4,
            iPadAir,
            Simulator,
            Unknown
        }

        [DllImport(Constants.SystemLibrary)]
        static extern int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string property, // name of the property
                                               IntPtr output, // output
                                               IntPtr oldLen, // IntPtr.Zero
                                               IntPtr newp, // IntPtr.Zero
                                               uint newlen // 0
                                              );

        public static HardwareModel Version
        {
            get
            {
                // Query length
                var lengthPtr = Marshal.AllocHGlobal(sizeof(int));
                sysctlbyname("hw.machine", IntPtr.Zero, lengthPtr, IntPtr.Zero, 0);

                var length = Marshal.ReadInt32(lengthPtr);

                // Empty string?
                if (length == 0)
                {
                    Marshal.FreeHGlobal(lengthPtr);
                    return HardwareModel.Unknown;
                }

                // get the hardware string
                var hardwareStrPtr = Marshal.AllocHGlobal(length);
                sysctlbyname("hw.machine", hardwareStrPtr, lengthPtr, IntPtr.Zero, 0);

                // convert the native string into a C# string
                var hardwareStr = Marshal.PtrToStringAnsi(hardwareStrPtr);
                var ret = HardwareModel.Unknown;


                // determine which hardware we are running
                if (hardwareStr == "iPhone1,1")
                    ret = HardwareModel.iPhone1G;
                else if (hardwareStr == "iPhone1,2")
                    ret = HardwareModel.iPhone3G;
                else if (hardwareStr == "iPhone2,1")
                    ret = HardwareModel.iPhone3GS;
                else if (hardwareStr.StartsWith("iPhone3"))
                    ret = HardwareModel.iPhone4;
                else if (hardwareStr.StartsWith("iPhone4"))
                    ret = HardwareModel.iPhone4S;
                else if (hardwareStr.StartsWith("iPhone5"))
                    ret = HardwareModel.iPhone5;
                else if (hardwareStr.StartsWith("iPhone6"))
                    ret = HardwareModel.iPhone5S;
                else if (hardwareStr.StartsWith("iPod1"))
                    ret = HardwareModel.iPod1G;
                else if (hardwareStr.StartsWith("iPod2"))
                    ret = HardwareModel.iPod2G;
                else if (hardwareStr.StartsWith("iPod3"))
                    ret = HardwareModel.iPod3G;
                else if (hardwareStr.StartsWith("iPod4"))
                    ret = HardwareModel.iPod4G;
                else if (hardwareStr.StartsWith("iPod5"))
                    ret = HardwareModel.iPod5G;
                else if (hardwareStr == "iPad1,1")
                    ret = HardwareModel.iPad;
                else if (hardwareStr.StartsWith("iPad2"))
                    ret = HardwareModel.iPad2;
                else if (hardwareStr == "iPad3,1" || hardwareStr == "iPad3,2" || hardwareStr == "iPad3,3")
                    ret = HardwareModel.iPad3;
                else if (hardwareStr == "iPad3,4" || hardwareStr == "iPad3,5" || hardwareStr == "iPad3,6")
                    ret = HardwareModel.iPad4;
                else if (hardwareStr.StartsWith("iPad4"))
                    ret = HardwareModel.iPadAir;
                else if (hardwareStr == "i386")
                    ret = HardwareModel.Simulator;

                // Cleanup
                Marshal.FreeHGlobal(lengthPtr);
                Marshal.FreeHGlobal(hardwareStrPtr);

                return ret;
            }
        }
    }
}
#endif
