// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;
using Stride.Core.Quantum.References;

namespace Stride.Core.Presentation.Quantum.Tests.Helpers
{
    public static class Types
    {
        public class TestPropertyProvider : IPropertyProviderViewModel
        {
            private readonly IObjectNode rootNode;

            public TestPropertyProvider(IObjectNode rootNode)
            {
                this.rootNode = rootNode;
            }
            public bool CanProvidePropertiesViewModel => true;

            public IObjectNode GetRootNode()
            {
                return rootNode;
            }

            bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

            bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
        }

        public class SimpleObject
        {
            public string Name { get; set; }

            public string Nam { get; set; } // To test scenario when Name.StartsWith(Nam) is true
        }

        public class DependentPropertyContainer
        {
            public string Title { get; set; }

            public SimpleObject Instance { get; set; }
        }

        public class SimpleType
        {
            public string String { get; set; }
        }

        public class ClassWithRef
        {
            [Display(1)]
            public string String { get; set; }
            [Display(2)]
            public ClassWithRef Ref { get; set; }
        }

        public class ClassWithCollection
        {
            [Display(1)]
            public string String { get; set; }
            [Display(2)]
            public List<string> List { get; set; } = new List<string>();
        }

        public class ClassWithRefCollection
        {
            [Display(1)]
            public string String { get; set; }
            [Display(2)]
            public List<SimpleType> List { get; set; } = new List<SimpleType>();
        }
    }
}
