// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Games.AutoTesting;

/// <summary>
/// Marks a class as the screenshot-test driver for a sample. The class must implement
/// <see cref="IScreenshotTest"/> and have a public parameterless constructor. Exactly one
/// such class may exist in the entry assembly when <c>StrideAutoTesting=true</c>.
/// </summary>
/// <remarks>
/// The optional <see cref="TemplateId"/> identifies which Stride sample template this fixture
/// targets — orchestrators read it to look the template up in <c>TemplateManager.FindTemplates</c>
/// without depending on filename conventions. Stays string-typed because attribute arguments must
/// be compile-time constants and <see cref="Guid"/> isn't.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ScreenshotTestAttribute : Attribute
{
    public string? TemplateId { get; init; }
}
