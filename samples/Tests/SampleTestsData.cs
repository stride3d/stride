// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Stride.Core;
using Stride.Core.Assets;
using Xunit;

// We run test one by one (various things are not thread-safe)
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Stride.Samples.Tests
{
    class SampleTestsData
    {
#if TEST_ANDROID
        public const PlatformType TestPlatform = PlatformType.Android;
#elif TEST_IOS
        public const PlatformType TestPlatform = PlatformType.iOS;
#else
        public const PlatformType TestPlatform = PlatformType.Windows;
#endif
    }
}
