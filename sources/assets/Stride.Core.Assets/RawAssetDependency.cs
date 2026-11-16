// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets;

/// <summary>
/// A file that importing a raw asset reads besides the raw asset itself, such as a texture of a model.
/// </summary>
/// <param name="Source">Where the file is now.</param>
/// <param name="RelativePath">
/// Where the importer reads the file, relative to the folder of the raw asset. A copy of the raw asset needs the file at
/// that place relative to the copy.
/// </param>
public readonly record struct RawAssetDependency(UFile Source, string RelativePath);
