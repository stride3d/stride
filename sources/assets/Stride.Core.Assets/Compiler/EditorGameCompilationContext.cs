// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Compiler;

// The namespace it had in Stride.Editor, which forwards the type here: plugins built against Stride.Editor still find it
namespace Stride.Editor.Preview;

/// <summary>
/// The compilation context of the editor's game (scene and prefab editors): an asset type may register a lighter compiler
/// for it, such as one that makes a placeholder of a sound or a video instead of encoding it.
/// </summary>
/// <remarks>
/// It lives beside <see cref="AssetCompilationContext"/> so that an Assets package, which always loads, can register such a
/// compiler without referencing the editor.
/// </remarks>
public class EditorGameCompilationContext : AssetCompilationContext;
