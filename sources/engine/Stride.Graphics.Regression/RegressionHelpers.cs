// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Stride.Core.LZ4;

#if STRIDE_PLATFORM_UWP
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.System.Profile;
using Windows.Security.ExchangeActiveSyncProvisioning;
#endif

namespace Stride.Graphics.Regression
{
    public partial class TestRunner
    {
        public const string StrideVersion = "STRIDE_VERSION";

        public const string StrideBuildNumber = "STRIDE_BUILD_NUMBER";

        public const string StrideTestName = "STRIDE_TEST_NAME";

        public const string StrideBranchName = "STRIDE_BRANCH_NAME";
    }

    enum ImageServerMessageType
    {
        ConnectionFinished = 0,
        SendImage = 1,
        RequestImageComparisonStatus = 2,
    }
    
    public class PlatformPermutator
    {
        public static ImageTestResultConnection GetDefaultImageTestResultConnection()
        {
            var result = new ImageTestResultConnection();

            // TODO: Check build number in environment variables
            result.BuildNumber = -1;

#if STRIDE_PLATFORM_WINDOWS_DESKTOP
            result.Platform = "Windows";
            result.Serial = Environment.MachineName;
    #if STRIDE_GRAPHICS_API_DIRECT3D12
            result.DeviceName = "Direct3D12";
    #elif STRIDE_GRAPHICS_API_DIRECT3D11
            result.DeviceName = "Direct3D";
    #elif STRIDE_GRAPHICS_API_OPENGLES
            result.DeviceName = "OpenGLES";
    #elif STRIDE_GRAPHICS_API_OPENGL
            result.DeviceName = "OpenGL";
    #elif STRIDE_GRAPHICS_API_VULKAN
            result.DeviceName = "Vulkan";
    #endif
#elif STRIDE_PLATFORM_ANDROID
            result.Platform = "Android";
            result.DeviceName = Android.OS.Build.Manufacturer + " " + Android.OS.Build.Model;
            result.Serial = Android.OS.Build.Serial ?? "Unknown";
#elif STRIDE_PLATFORM_IOS
            result.Platform = "iOS";
            result.DeviceName = iOSDeviceType.Version.ToString();
            result.Serial = UIKit.UIDevice.CurrentDevice.Name;
#elif STRIDE_PLATFORM_UWP
            result.Platform = "UWP";
            var deviceInfo = new EasClientDeviceInformation();
            result.DeviceName = deviceInfo.SystemManufacturer + " " + deviceInfo.SystemProductName;
            try
            {
                result.Serial = deviceInfo.Id.ToString();
            }
            catch (Exception)
            {
                // Ignored on UWP
            }
#endif

            return result;
        }

        public static string GetCurrentPlatformName()
        {
            return GetPlatformName(GetPlatform());
        }

        public static string GetPlatformName(TestPlatform platform)
        {
            switch (platform)
            {
                case TestPlatform.WindowsDx:
                    return "Windows_Direct3D11";
                case TestPlatform.WindowsOgl:
                    return "Windows_OpenGL";
                case TestPlatform.WindowsOgles:
                    return "Windows_OpenGLES";
                case TestPlatform.Android:
                    return "Android";
                case TestPlatform.Ios:
                    return "IOS";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static TestPlatform GetPlatform()
        {
#if STRIDE_PLATFORM_ANDROID
            return TestPlatform.Android;
#elif STRIDE_PLATFORM_IOS
            return TestPlatform.Ios;
#elif STRIDE_GRAPHICS_API_NULL
            return TestPlatform.None;
#elif STRIDE_GRAPHICS_API_DIRECT3D
            return TestPlatform.WindowsDx;
#elif STRIDE_GRAPHICS_API_OPENGLES
            return TestPlatform.WindowsOgles;
#elif STRIDE_GRAPHICS_API_OPENGL
            return TestPlatform.WindowsOgl;
#elif STRIDE_GRAPHICS_API_VULKAN
            return TestPlatform.WindowsVulkan;
#endif

        }
    }

    [Flags]
    public enum ImageComparisonFlags
    {
        CopyOnShare = 1,
    }

    public class ImageTestResultConnection
    {
        public int BuildNumber;
        public string Platform;
        public string Serial;
        public string DeviceName;
        public string BranchName = "";
        public ImageComparisonFlags Flags;

        public void Read(BinaryReader reader)
        {
            Platform = reader.ReadString();
            BuildNumber = reader.ReadInt32();
            Serial = reader.ReadString();
            DeviceName = reader.ReadString();
            BranchName = reader.ReadString();
            Flags = (ImageComparisonFlags)reader.ReadInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Platform);
            writer.Write(BuildNumber);
            writer.Write(Serial);
            writer.Write(DeviceName);
            writer.Write(BranchName);
            writer.Write((int)Flags);
        }
    }

    public struct ImageInformation
    {
        public int Width;
        public int Height;
        public int TextureSize;
        public int BaseVersion;
        public int CurrentVersion;
        public int FrameIndex;
        public TestPlatform Platform;
        public PixelFormat Format;
    }

    public enum TestPlatform
    {
        None,
        WindowsDx,
        WindowsOgl,
        WindowsOgles,
        WindowsVulkan,
        Android,
        Ios
    }
}
