#region License

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// This file is distributed under MIT License. See LICENSE.md for details.
//
// SLNTools
// Copyright (c) 2009
// by Christian Warren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion

namespace Stride.Core.VisualStudio;

/// <summary>
/// A section defined in a <see cref="Project"/>
/// </summary>
public sealed class Section
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Section"/> class.
    /// </summary>
    /// <param name="original">The original section to copy from.</param>
    private Section(Section original)
        : this(original.Name, original.SectionType, original.Step, original.Properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Section"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="sectionType">Type of the section.</param>
    /// <param name="step">The step.</param>
    /// <param name="properties">The property lines.</param>
    public Section(string name, string sectionType, string step, IEnumerable<PropertyItem> properties)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(name);
#else
        if (name is null) throw new ArgumentNullException(nameof(name));
#endif
        this.Name = name;
        SectionType = sectionType;
        Step = step;
        this.Properties = new PropertyItemCollection(properties);
    }

    /// <summary>
    /// Gets the name of the section.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <value>The properties.</value>
    public PropertyItemCollection Properties { get; }

    /// <summary>
    /// Gets or sets the type of the section.
    /// </summary>
    /// <value>The type of the section.</value>
    public string SectionType { get; set; }

    /// <summary>
    /// Gets or sets the step.
    /// </summary>
    /// <value>The step.</value>
    public string Step { get; set; }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns>Section.</returns>
    public Section Clone()
    {
        return new Section(this);
    }

    public override string ToString()
    {
        return $"{SectionType} '{Name}'";
    }
}
