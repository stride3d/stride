// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.StorageTool;

/// <summary>
/// Description of an object entry in the bundle.
/// </summary>
public class ObjectEntry
{
    public string Location { get; set; }

    public string Id { get; set; }

    public long Size { get; set; }

    public long SizeNotCompressed { get; set; }
}
