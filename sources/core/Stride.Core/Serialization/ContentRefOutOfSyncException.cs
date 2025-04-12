// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Serialization;

public class ContentRefOutOfSyncException() 
    : InvalidOperationException("Serialization contentRef indices are out of sync, assets need to be rebuilt, clean your projects and rebuild");
