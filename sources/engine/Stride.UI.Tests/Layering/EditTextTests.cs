// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI.Controls;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// A class that contains test functions for layering of the <see cref="EditText"/> class.
    /// </summary>
    [System.ComponentModel.Description("Tests for EditText layering")]
    public class EditTextTests
    {
        class DummyFont : SpriteFont { }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Fact]
        public void TestBasicInvalidations()
        {
            var services = new ServiceRegistry();
            services.AddService<IGame>(new Game());

            var edit = new EditText();
            edit.UIElementServices = new UIElementServices { Services = services };

            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(edit, () => edit.Font = new DummyFont());
            edit.Font = null;
            UIElementLayeringTests.TestMeasureInvalidation(edit, () => edit.MaxLines = 34);
            UIElementLayeringTests.TestMeasureInvalidation(edit, () => edit.MinLines = 34);
            UIElementLayeringTests.TestMeasureInvalidation(edit, () => edit.SelectedText = "toto");
            UIElementLayeringTests.TestMeasureInvalidation(edit, () => edit.Text = "titi");
            UIElementLayeringTests.TestMeasureInvalidation(edit, () => edit.MaxLength = 3); // text is modified

            // - test the properties that are not supposed to invalidate the object layout state
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.IsReadOnly = !edit.IsReadOnly);
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.ActiveImage = (SpriteFromTexture)new Sprite());
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.InactiveImage = (SpriteFromTexture)new Sprite());
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.TextColor = new Color(1, 2, 3, 4));
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.SelectionColor = new Color(1, 2, 3, 4));
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.CaretColor = new Color(1, 7, 3, 4));
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.CaretPosition = 34);
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.SelectionLength = 2);
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.SelectionStart = 0);
            UIElementLayeringTests.TestNoInvalidation(edit, () => edit.MaxLength = 34); // text is not modified
        }
    }
}
