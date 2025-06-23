// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;

public sealed class SettingsPropertyNodeUpdater : NodePresenterUpdaterBase
{
    public override void UpdateNode(INodePresenter node)
    {
        if (node.Value is PackageSettingsWrapper.SettingsKeyWrapper settingsKey)
        {
            var acceptableValues = settingsKey.Key.AcceptableValues.ToList();
            node.AttachedProperties.Add(SettingsData.HasAcceptableValuesKey, acceptableValues.Count > 0);
            node.AttachedProperties.Add(SettingsData.AcceptableValuesKey, acceptableValues);
        }
    }
}
