// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics.Regression;

/// <summary>
///   Marks a test method or class as allowing a specific GPU validation error that is a known false positive.
///   The error is suppressed during assertion if its text contains the specified substring.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class AllowGpuValidationErrorAttribute(GraphicsPlatform platform, string messageSubstring) : Attribute
{
    /// <summary>
    ///   The graphics platform this error is allowed on.
    /// </summary>
    public GraphicsPlatform Platform { get; } = platform;

    /// <summary>
    ///   A substring that must appear in the validation error message for it to be suppressed.
    /// </summary>
    public string MessageSubstring { get; } = messageSubstring;

    /// <summary>
    ///   An optional explanation of why this error is allowed.
    /// </summary>
    public string Reason { get; set; }
}
