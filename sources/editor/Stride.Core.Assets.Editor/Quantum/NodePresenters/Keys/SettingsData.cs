// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

public static class SettingsData
{
    public const string HasAcceptableValues = nameof(HasAcceptableValues);
    public const string AcceptableValues = nameof(AcceptableValues);
    public static readonly PropertyKey<bool> HasAcceptableValuesKey = new(HasAcceptableValues, typeof(SettingsData));
    public static readonly PropertyKey<IEnumerable<object>> AcceptableValuesKey = new(AcceptableValues, typeof(SettingsData));
}
