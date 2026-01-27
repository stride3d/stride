// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Shaders.Ast.Hlsl;

/// <summary>
///   A syntax node representing a HLSL interpolation qualifier.
/// </summary>
/// <seealso cref="Qualifier"/>
public class InterpolationQualifier : Qualifier
{
    #region Interpolation Qualifier Keys

    private const string CentroidKey = "centroid";
    private const string LinearKey = "linear";
    private const string NoPerspectiveKey = "noperspective";
    private const string NointerpolationKey = "nointerpolation";
    private const string SampleKey = "sample";

    #endregion

    /// <summary>
    ///   The <c>"centroid"</c> modifier, only valid for structure fields.
    ///   Indicates that interpolation should be performed at the centroid of the covered fragments,
    ///   which can help avoid artifacts at triangle edges.
    /// </summary>
    public static readonly InterpolationQualifier Centroid = new(CentroidKey);

    /// <summary>
    ///   The <c>"linear"</c> modifier, only valid for structure fields.
    ///   Specifies that interpolation should be linear in screen space.
    /// </summary>
    public static readonly InterpolationQualifier Linear = new(LinearKey);

    /// <summary>
    ///   The <c>"noperspective"</c> modifier, only valid for structure fields.
    ///   Indicates that interpolation should be performed without perspective correction,
    ///   useful for certain graphical effects.
    /// </summary>
    public static readonly InterpolationQualifier NoPerspective = new(NoPerspectiveKey);

    /// <summary>
    ///   The <c>"nointerpolation"</c> modifier.
    ///   Specifies that no interpolation should be performed; the value is copied directly from the corresponding vertex.
    /// </summary>
    public static readonly InterpolationQualifier Nointerpolation = new(NointerpolationKey);

    /// <summary>
    ///   The <c>"sample"</c> modifier, only valid for structure fields.
    ///   Indicates that interpolation should be performed at the exact sample location,
    ///   useful for multi-sample techniques (MSAA).
    /// </summary>
    public static readonly InterpolationQualifier Sample = new(SampleKey);


    /// <summary>
    ///   Initializes a new instance of the <see cref="InterpolationQualifier"/> class.
    /// </summary>
    public InterpolationQualifier() : base() { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="InterpolationQualifier"/> class.
    /// </summary>
    /// <param name="key">The name of the interpolation qualifier.</param>
    public InterpolationQualifier(object key) : base(key) { }


    /// <summary>
    ///   Parses the specified qualifier name into an interpolation qualifier.
    /// </summary>
    /// <param name="qualifierName">The name of the qualifier to parse.</param>
    /// <returns>
    ///   An interpolation <see cref="Qualifier"/>, or <see langword="null"/> if the qualifier name is not recognized.
    /// </returns>
    public static Qualifier? Parse(string qualifierName)
    {
        return qualifierName switch
        {
            CentroidKey => Centroid,
            LinearKey => Linear,
            NoPerspectiveKey => NoPerspective,
            NointerpolationKey => Nointerpolation,
            SampleKey => Sample,

            _ => null
        };
    }
}
