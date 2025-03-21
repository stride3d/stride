// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.CodeEditor.Rider;
using Stride.Core.CodeEditor.VisualStudio;
using Stride.Core.CodeEditor.VSCode;

namespace Stride.Core.CodeEditor;

public static class IDEInfoVersions
{
    public static IEnumerable<IDEInfo> AvailableIDEs()
    {
        return VisualStudioVersions.AvailableInstances
            .Concat(RiderVersions.AvailableInstances)
            .Concat(VSCodeVersions.AvailableInstances);
    }
}
