// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.Services;

/// <summary>
/// What the session objects need from the plugins: the primitive types of the property graphs and the settings that the plugins add
/// to the properties of the packages.
/// </summary>
/// <remarks>
/// The editor's plugin service (<c>IAssetsPluginService</c>) implements it. The session objects find it in the service provider of the
/// session.
/// </remarks>
public interface IPresentationPluginService
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
