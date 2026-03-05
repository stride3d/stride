// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Shaders.Ast;

/// <summary>
///   A syntax node that represent one or more <strong>storage qualifiers</strong>.
/// </summary>
/// <remarks>
///   Storage qualifiers are used to qualify types or variables in Shader code,
///   like <c>const</c>, <c>in</c>, <c>out</c>, <c>uniform</c>, etc.
/// </remarks>
public partial class Qualifier : CompositeEnum
{
    /// <summary>
    ///   An empty qualifier.
    /// </summary>
    public static readonly Qualifier None = new(key: string.Empty);


    /// <summary>
    ///   Gets or sets a value indicating whether the syntax node is a post qualifier,
    ///   i.e. appears after the type it qualifies (e.g. like semantics or register qualifiers).
    /// </summary>
    public bool IsPost { get; set; }


    /// <summary>
    ///   Initializes a new instance of the <see cref="Qualifier"/> class.
    /// </summary>
    public Qualifier() : base(isFlag: true) { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="Qualifier"/> class.
    /// </summary>
    /// <param name="key">The name of the qualifier.</param>
    public Qualifier(object key) : base(key, isFlag: true) { }


    /// <summary>
    ///   Returns a string representation of the qualifier, filtering by whether they are post or pre qualifiers.
    /// </summary>
    /// <param name="isPost">
    ///   A value indicating whether to return post qualifiers (if <see langword="true"/>),
    ///   or pre qualifiers (if <see langword="false"/>).
    /// </param>
    /// <returns>A string representation of the qualifier.</returns>
    public string ToString(bool isPost)
    {
        var str = ToString<Qualifier>(qualifier => qualifier.IsPost == isPost);

        if (!string.IsNullOrEmpty(str))
        {
            return isPost ? $" {str}" : $"{str} ";
        }
        return string.Empty;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return ToString<Qualifier>(qualifier => true);
    }

    #region Operators

    public static Qualifier operator &(Qualifier left, Qualifier right)
    {
        return OperatorAnd(left, right);
    }

    public static Qualifier operator |(Qualifier left, Qualifier right)
    {
        return OperatorOr(left, right);
    }

    public static Qualifier operator ^(Qualifier left, Qualifier right)
    {
        return OperatorXor(left, right);
    }

    #endregion
}
