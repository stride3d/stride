// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

/// <summary>
/// The attached properties of a property whose value can go back to its default (see <see cref="MemberDefaultValues"/>).
/// </summary>
public static class DefaultValueData
{
    /// <summary>
    /// The name of the attached property that says whether the value of the property, or of the property it inlines, differs
    /// from its default.
    /// </summary>
    public const string DiffersFromDefault = nameof(DiffersFromDefault);

    /// <summary>
    /// Whether the value of the property, or of the property it inlines, differs from its default. A property whose default
    /// is unknown, or that has a base (an archetype, a prefab), has none. Several objects combined: whether one of them differs.
    /// </summary>
    public static readonly PropertyKey<bool> DiffersFromDefaultKey = new(DiffersFromDefault, typeof(DefaultValueData), new PropertyCombinerMetadata(CombineAny));

    private static object CombineAny(IEnumerable<object> values) => values.Any(x => x is true);
}
