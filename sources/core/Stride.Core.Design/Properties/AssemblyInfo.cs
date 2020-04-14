// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Stride.Core.Design.Serializers")]
[assembly: InternalsVisibleTo("Stride.Engine")]
[assembly: InternalsVisibleTo("Stride.Engine.Step1")]
[assembly: InternalsVisibleTo("Stride.Core.Tests")]
[assembly: InternalsVisibleTo("Stride.Core.Design.Tests")]
[assembly: InternalsVisibleTo("Stride.Core.Presentation.Tests")]
// looks like whenever we open the generated iOS solution with visual studio, it removes the dot in the assembly name -_-
#if STRIDE_PLATFORM_IOS
[assembly: InternalsVisibleTo("StrideCoreTests")]
#endif
