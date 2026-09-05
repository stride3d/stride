// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;

namespace Stride.Core.Assets.Compiler;

/// <summary>
/// An assembly-level attribute declaring a compiler for an asset type, for compilers with no class of their own.
/// </summary>
public interface IAssetCompilerDeclaration
{
    Type AssetType { get; }

    Type CompilationContext { get; }

    IAssetCompiler CreateCompiler(Assembly assembly);
}
