// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Internal;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestTypeHelpers
    {
        private class PreviewBase<T> { }

        private class SpecificPreview : PreviewBase<int> { }

        private class OtherPreview : PreviewBase<string> { }

        private class UnrelatedPreview { }

        [Fact]
        public void TryGetTypeOrBaseMatchesAnOpenGenericBase()
        {
            var map = new Dictionary<Type, Type>
            {
                [typeof(PreviewBase<>)] = typeof(object),
                [typeof(SpecificPreview)] = typeof(string),
            };

            Assert.Equal(typeof(string), TypeHelpers.TryGetTypeOrBase(typeof(SpecificPreview), map));
            Assert.Equal(typeof(object), TypeHelpers.TryGetTypeOrBase(typeof(OtherPreview), map));
            Assert.Null(TypeHelpers.TryGetTypeOrBase(typeof(UnrelatedPreview), map));
        }
    }
}
