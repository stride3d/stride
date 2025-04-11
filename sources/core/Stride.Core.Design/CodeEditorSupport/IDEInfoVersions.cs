// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.CodeEditorSupport.Rider;
using Stride.Core.CodeEditorSupport.VisualStudio;
using Stride.Core.CodeEditorSupport.VSCode;

namespace Stride.Core.CodeEditorSupport;

public static class IDEInfoVersions
{
    public static IEnumerable<IDEInfo> AvailableIDEs()
    {
        return [..VisualStudioVersions.AvailableInstances, ..RiderVersions.AvailableInstances, ..VSCodeVersions.AvailableInstances];
    }
}
