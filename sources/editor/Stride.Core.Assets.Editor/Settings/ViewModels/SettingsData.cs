// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Assets.Editor.Settings.ViewModels
{
    public class SettingsData
    {
        public const string HasAcceptableValues = nameof(HasAcceptableValues);
        public const string AcceptableValues = nameof(AcceptableValues);
        public static readonly PropertyKey<bool> HasAcceptableValuesKey = new PropertyKey<bool>(HasAcceptableValues, typeof(SettingsData));
        public static readonly PropertyKey<IEnumerable<object>> AcceptableValuesKey = new PropertyKey<IEnumerable<object>>(AcceptableValues, typeof(SettingsData));

    }
}
