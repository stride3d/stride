// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Xenko.Core.Design.Serializers")]
[assembly: InternalsVisibleTo("Xenko.Engine")]
[assembly: InternalsVisibleTo("Xenko.Engine.Step1")]
[assembly: InternalsVisibleTo("Xenko.Core.Tests")]
[assembly: InternalsVisibleTo("Xenko.Core.Design.Tests")]
[assembly: InternalsVisibleTo("Xenko.Core.Presentation.Tests")]
// looks like whenever we open the generated iOS solution with visual studio, it removes the dot in the assembly name -_-
#if XENKO_PLATFORM_IOS
[assembly: InternalsVisibleTo("XenkoCoreTests")]
#endif
