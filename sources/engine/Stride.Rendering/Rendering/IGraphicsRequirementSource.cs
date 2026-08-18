// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;

namespace Stride.Rendering;

/// <summary>
///   A renderer implements this to declare capabilities the device may not have. The compositor collects
///   every declaration and reports them together.
/// </summary>
/// <remarks>
///   Implement this only for a need the renderer can answer when it initializes. A need that depends on
///   the scene content belongs to the graphics profile the content was built against. Declaring does not
///   replace the branch. A renderer still checks the capability where it uses it.
///   <para>
///     The compositor reports at the end of the first frame, not before it. An image effect initializes
///     when it first draws, so no earlier point has every declaration.
///   </para>
/// </remarks>
public interface IGraphicsRequirementSource
{
    /// <summary>
    ///   Declares what this renderer needs from the device.
    /// </summary>
    /// <param name="collector">Collects the declaration. Read its device to evaluate the condition.</param>
    void DeclareRequirements(GraphicsRequirementCollector collector);
}
