// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.DotRecast.Definitions;

public enum NavMeshLayer
{
    None = 0,
    Layer1 = 1,
    Layer2 = 2,
    Layer3 = 4,
    Layer4 = 8,
    Layer5 = 16,
    Layer6 = 32,
    Layer7 = 64,
    Layer8 = 128,
    Layer9 = 256,
    Layer10 = 512,
    Layer11 = 1024,
    Layer12 = 2048,
    Layer13 = 4096,
    Layer14 = 8192,
    Layer15 = 16384,
    Layer16 = 32768,
}

[Flags]
public enum NavMeshLayerGroup
{
    None = 0,
    Layer1 = 1,
    Layer2 = 2,
    Layer3 = 4,
    Layer4 = 8,
    Layer5 = 16,
    Layer6 = 32,
    Layer7 = 64,
    Layer8 = 128,
    Layer9 = 256,
    Layer10 = 512,
    Layer11 = 1024,
    Layer12 = 2048,
    Layer13 = 4096,
    Layer14 = 8192,
    Layer15 = 16384,
    Layer16 = 32768,
}
