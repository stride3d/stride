// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Quantum;
using Xenko.Core.Quantum.References;

namespace Xenko.Core.Presentation.Quantum.Tests.Helpers
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
