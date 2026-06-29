// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.AutoTesting;

[AttributeUsage(AttributeTargets.Class)]
public sealed class UITestAttribute : Attribute
{
    /// <summary>
    /// Optional template GUID. The test runner uses it to regenerate the matching sample on disk
    /// before launching GameStudio; the harness itself does not consult this field.
    /// </summary>
    public string? SampleTemplateId { get; set; }
}
