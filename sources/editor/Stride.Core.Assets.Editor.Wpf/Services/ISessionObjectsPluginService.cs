// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// What the session objects (packages, assets...) need from the plugins. The editor's plugin service (<see cref="IAssetsPluginService"/>) provides it.
/// </summary>
public interface ISessionObjectsPluginService
{
    /// <summary>
    /// Gets the types that the plugins register as primitive types, which the property grid does not expand.
    /// </summary>
    IEnumerable<Type> GetPrimitiveTypes(ISessionViewModel session);

    /// <summary>
    /// Gets the settings that the plugins add to the properties of the packages.
    /// </summary>
    IEnumerable<PackageSettingsEntry> GetProfileSettings();
}
