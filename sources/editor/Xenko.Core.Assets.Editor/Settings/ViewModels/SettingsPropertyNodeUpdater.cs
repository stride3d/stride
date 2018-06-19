// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Editor.Components.Properties;
using Xenko.Core.Presentation.Quantum.Presenters;

namespace Xenko.Core.Assets.Editor.Settings.ViewModels
{
    internal class SettingsPropertyNodeUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            var settingsKey = node.Value as PackageSettingsWrapper.SettingsKeyWrapper;
            if (settingsKey != null)
            {
                var acceptableValues = settingsKey.Key.AcceptableValues.ToList();
                node.AttachedProperties.Add(SettingsData.HasAcceptableValuesKey, acceptableValues.Count > 0);
                node.AttachedProperties.Add(SettingsData.AcceptableValuesKey, acceptableValues);
            }
        }
    }
}
