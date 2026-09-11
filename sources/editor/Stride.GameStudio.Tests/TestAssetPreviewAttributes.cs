// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Reflection;
using Stride.Assets.Presentation.Preview;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Assets.Presentation.ViewModel.Preview;
using Stride.Editor.Annotations;
using Xunit;

namespace Stride.GameStudio.Tests
{
    /// <summary>
    /// A preview view or view model deriving from another one serves its own previews only: registering the base's too
    /// would collide with the base's own registration (a plugin's view deriving from the model view).
    /// </summary>
    public class TestAssetPreviewAttributes
    {
        [AssetPreviewView<TexturePreview>]
        private class DerivedPreviewView : ModelPreviewView
        {
        }

        private class DerivedPreviewViewModel : ModelPreviewViewModel
        {
            public DerivedPreviewViewModel()
                : base(null)
            {
            }
        }

        [Fact]
        public void DerivedPreviewViewServesItsOwnPreviewsOnly()
        {
            var previewTypes = typeof(DerivedPreviewView).GetCustomAttributes<AssetPreviewViewAttribute>().Select(x => x.AssetPreviewType);
            Assert.Equal([typeof(TexturePreview)], previewTypes);
        }

        [Fact]
        public void DerivedPreviewViewModelDoesNotServeItsBasePreview()
        {
            Assert.Empty(typeof(DerivedPreviewViewModel).GetCustomAttributes<AssetPreviewViewModelAttribute>());
        }
    }
}
