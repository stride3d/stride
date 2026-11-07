// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Settings.ViewModels
{
    internal class SettingsPropertyNodeUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            switch (node.Value)
            {
                case PackageSettingsWrapper.SettingsKeyWrapper settingsKey:
                    node.DisplayName = settingsKey.Key.DisplayName.GetFileName();
                    var acceptableValues = settingsKey.Key.AcceptableValues.ToList();
                    node.AttachedProperties.Add(SettingsData.HasAcceptableValuesKey, acceptableValues.Count > 0);
                    node.AttachedProperties.Add(SettingsData.AcceptableValuesKey, acceptableValues);
                    break;

                case SettingsCommand command:
                    node.DisplayName = command.DisplayName.GetFileName();
                    break;
            }

            // The value of a numeric setting gets the range of its type, as NumericValueNodeUpdater gives it to the members of an
            // asset only: without it, the number box of the Avalonia settings window clamps the value to 0
            if (node.Parent?.Value is PackageSettingsWrapper.SettingsKeyWrapper && node.Type.IsNumeric())
            {
                node.AttachedProperties.Set(NumericData.MinimumKey, node.Type.GetField(nameof(int.MinValue))?.GetValue(null));
                node.AttachedProperties.Set(NumericData.MaximumKey, node.Type.GetField(nameof(int.MaxValue))?.GetValue(null));
                if (node.Type.IsIntegral())
                    node.AttachedProperties.Set(NumericData.DecimalPlacesKey, 0);
            }
        }
    }
}
