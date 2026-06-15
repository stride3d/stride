// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Stride.Assets.Templates;

/// <summary>UI hook for the Add-Library flow; null result = cancel.</summary>
public interface IAddLibraryParameterPrompt
{
    Task<AddLibraryResult?> PromptAsync(string defaultName, Func<string, bool> isNameTaken);
}

public sealed record AddLibraryResult(string LibraryName, string Namespace);
