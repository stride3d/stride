// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Data;

namespace Stride.Input;

/// <summary>
/// Game settings for input.
/// </summary>
[DataContract]
[Display("Input")]
public sealed class InputSettings : Configuration
{
    /// <summary>
    /// Gets or sets a value indicating whether captured input is hidden from scripts and systems.
    /// </summary>
    /// <userdoc>
    /// Turns input masking on or off. When enabled, input that an owner has captured is not seen by scripts and systems.
    /// The UI is the usual owner: a click on a button or typing in a text box is not also seen by your scripts. Other
    /// owners, such as a debug console or an overlay, follow the same setting.
    /// </userdoc>
    [DataMember(10)]
    [Display("UI focus withholds input")]
    public bool MaskCapturedInput { get; set; } = true;
}
