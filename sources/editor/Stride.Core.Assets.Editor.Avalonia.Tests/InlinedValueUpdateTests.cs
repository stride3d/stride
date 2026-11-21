// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Quantum;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Views;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// An inlined value (the color of a light, shown in the row of its color provider) follows the writes of its value without the
/// row being built again.
/// </summary>
public sealed class InlinedValueUpdateTests
{
    [AvaloniaFact]
    public void TheInlinedColorFollowsSeveralWritesWithoutRebuildingItsRow()
    {
        DefaultProviders.Register();
        var context = new NodeViewModelContext(new AssetNodeContainer(), new InlineMemberNodeUpdater());
        var light = new ColoredLight();
        var color = context.MemberOf(light, nameof(ColoredLight.Color));
        var inlined = color.GetChild(InlineData.InlinedProperty)!;
        var presenter = inlined.NodePresenters.Single();
        var window = new Window { Content = new ContentControl { Content = color, ContentTemplate = PropertyViewHelper.EditorProviders } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var editor = Assert.Single(window.GetVisualDescendants().OfType<Color3Editor>());
        var valueNode = context.NodeOf(light.Color)[nameof(ColorProvider.Value)];
        try
        {
            for (var i = 1; i <= 5; i++)
            {
                // Whole bytes, so that the text of the editor (#RRGGBB) gives the same value back
                var expected = new Color3(i * 40 / 255f, (255 - i * 40) / 255f, i * 10 / 255f);
                if (i <= 3)
                {
                    valueNode.Update(expected);
                }
                else
                {
                    // As the values of a running game: several writes in a batch
                    using (presenter.Factory.BeginBatch())
                    {
                        valueNode.Update(new Color3(1, 1, 1));
                        valueNode.Update(expected);
                    }
                }
                Dispatcher.UIThread.RunJobs();

                Assert.Same(editor, Assert.Single(window.GetVisualDescendants().OfType<Color3Editor>()));
                Assert.Same(inlined, color.GetChild(InlineData.InlinedProperty));
                Assert.Same(presenter, inlined.NodePresenters.Single());
                Assert.Equal(expected, editor.Value);
                Assert.Equal(expected, light.Color.Value);
            }
        }
        finally
        {
            window.Close();
            color.Root.Destroy();
        }
    }

    public sealed class ColoredLight
    {
        public ColorProvider Color { get; set; } = new();
    }

    public sealed class ColorProvider
    {
        [InlineProperty]
        public Color3 Value { get; set; } = new(1, 0.5f, 0.25f);
    }
}
