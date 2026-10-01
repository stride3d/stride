// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Data;

namespace Stride.Input;

/// <summary>
///   Game settings for input.
/// </summary>
[DataContract]
[Display("Input")]
public sealed class InputSettings : Configuration
{
    /// <summary>
    ///   Gets or sets a value indicating whether input captured by UI layers is hidden from game code.
    /// </summary>
    /// <userdoc>
    ///   When enabled, input that the UI uses, such as a click on a button or typing in a text box, is not seen by game scripts.
    /// </userdoc>
    [DataMember(10)]
    [Display("Hide captured input from the game")]
    public bool MaskCapturedInput { get; set; } = true;
}
