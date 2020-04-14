// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Stride.Core.IO;

namespace Stride.Assets.Presentation.AssetEditors
{
    /// <summary>
    /// Provides information about scripts in loaded assemblies.
    /// </summary>
    public interface IScriptSourceCodeResolver
    {
        /// <summary>
        /// Gets script types that exist in a source file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        IEnumerable<Type> GetTypesFromSourceFile(UFile file);

        Compilation LatestCompilation { get; }

        event EventHandler LatestCompilationChanged;
    }
}
