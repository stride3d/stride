// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.Tests.Helpers;

namespace Stride.Core.Presentation.Quantum.Tests
{
    // TODO: this class should be rewritten to properly match the new design of dependent properties, which is using hard-link between nodes instead of path-based.
    public class TestDependentProperties
    {
        private const string Title = nameof(Types.DependentPropertyContainer.Title);
        private const string Instance = nameof(Types.DependentPropertyContainer.Instance);
        private const string Name = nameof(Types.SimpleObject.Name);
        private const string Nam = nameof(Types.SimpleObject.Nam);

        private const string TestDataKey = "TestData";
        private const string UpdateCountKey = "UpdateCount";
        private static readonly PropertyKey<string> TestData = new PropertyKey<string>(TestDataKey, typeof(TestDependentProperties));
        private static readonly PropertyKey<int> UpdateCount = new PropertyKey<int>(UpdateCountKey, typeof(TestDependentProperties));

        private abstract class DependentPropertiesUpdater : NodePresenterUpdaterBase
        {
            private int count;
            protected abstract bool IsRecursive { get; }

            public override void UpdateNode(INodePresenter node)
            {
                if (node.Name == nameof(Types.DependentPropertyContainer.Title))
                {
                    var instance = (Types.DependentPropertyContainer)node.Root.Value;
                    node.AttachedProperties.Set(TestData, instance.Instance.Name);
                    node.AttachedProperties.Set(UpdateCount, count++);
                }
            }

            public override void FinalizeTree(INodePresenter root)
            {
                var node = root[Title];
                var dependencyNode = GetDependencyNode(node.Root);
                node.AddDependency(dependencyNode, IsRecursive);
            }

            protected abstract INodePresenter GetDependencyNode(INodePresenter rootNode);
        }

        private class SimpleDependentPropertiesUpdater : DependentPropertiesUpdater
        {
            protected override bool IsRecursive => false;

            protected override INodePresenter GetDependencyNode(INodePresenter rootNode)
            {
                return rootNode[Instance][Name];
            }
        }

        private class RecursiveDependentPropertiesUpdater : DependentPropertiesUpdater
        {
            protected override bool IsRecursive => true;

            protected override INodePresenter GetDependencyNode(INodePresenter rootNode)
            {
                return rootNode[Instance];
            }
        }

        [Fact]
        public void TestSimpleDependency()
        {
            var container = new Types.DependentPropertyContainer { Title = "Title", Instance = new Types.SimpleObject { Name = "Test" } };
            var testContext = new TestContainerContext();
            var instanceContext = testContext.CreateInstanceContext(container);
            testContext.GraphViewModelService.AvailableUpdaters.Add(new SimpleDependentPropertiesUpdater());
            var viewModel = instanceContext.CreateViewModel();
            var titleNode = viewModel.RootNode.GetChild(Title);
            var nameNode = viewModel.RootNode.GetChild(Instance).GetChild(Name);

            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("Test", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(0, titleNode.AssociatedData[UpdateCountKey]);

            nameNode.NodeValue = "NewValue";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(1, titleNode.AssociatedData[UpdateCountKey]);

            nameNode.NodeValue = "NewValue2";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue2", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(2, titleNode.AssociatedData[UpdateCountKey]);
        }

        [Fact(Skip = "Review why this test is failing and refactor to match new design of dependent properties.")]
        public void TestSimpleDependencyChangeParent()
        {
            var container = new Types.DependentPropertyContainer { Title = "Title", Instance = new Types.SimpleObject { Name = "Test" } };
            var testContext = new TestContainerContext();
            var instanceContext = testContext.CreateInstanceContext(container);
            testContext.GraphViewModelService.AvailableUpdaters.Add(new SimpleDependentPropertiesUpdater());
            var viewModel = instanceContext.CreateViewModel();
            var titleNode = viewModel.RootNode.GetChild(Title);
            var instanceNode = viewModel.RootNode.GetChild(Instance);

            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("Test", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(0, titleNode.AssociatedData[UpdateCountKey]);

            instanceNode.NodeValue = new Types.SimpleObject { Name = "NewValue" };
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(1, titleNode.AssociatedData[UpdateCountKey]);

            instanceNode.NodeValue = new Types.SimpleObject { Name = "NewValue2" };
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue2", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(2, titleNode.AssociatedData[UpdateCountKey]);
        }

        [Fact]
        public void TestRecursiveDependency()
        {
            var container = new Types.DependentPropertyContainer { Title = "Title", Instance = new Types.SimpleObject { Name = "Test" } };
            var testContext = new TestContainerContext();
            var instanceContext = testContext.CreateInstanceContext(container);
            testContext.GraphViewModelService.AvailableUpdaters.Add(new RecursiveDependentPropertiesUpdater());
            var viewModel = instanceContext.CreateViewModel();
            var titleNode = viewModel.RootNode.GetChild(Title);
            var instanceNode = viewModel.RootNode.GetChild(Instance);

            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("Test", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(0, titleNode.AssociatedData[UpdateCountKey]);

            instanceNode.NodeValue = new Types.SimpleObject { Name = "NewValue" };
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(1, titleNode.AssociatedData[UpdateCountKey]);

            instanceNode.NodeValue = new Types.SimpleObject { Name = "NewValue2" };
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue2", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(2, titleNode.AssociatedData[UpdateCountKey]);
        }

        [Fact(Skip = "Review why this test is failing and refactor to match new design of dependent properties.")]
        public void TestRecursiveDependencyChangeChild()
        {
            var container = new Types.DependentPropertyContainer { Title = "Title", Instance = new Types.SimpleObject { Name = "Test" } };
            var testContext = new TestContainerContext();
            var instanceContext = testContext.CreateInstanceContext(container);
            testContext.GraphViewModelService.AvailableUpdaters.Add(new RecursiveDependentPropertiesUpdater());
            var viewModel = instanceContext.CreateViewModel();
            var titleNode = viewModel.RootNode.GetChild(Title);
            var nameNode = viewModel.RootNode.GetChild(Instance).GetChild(Name);

            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("Test", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(0, titleNode.AssociatedData[UpdateCountKey]);

            nameNode.NodeValue = "NewValue";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(1, titleNode.AssociatedData[UpdateCountKey]);

            nameNode.NodeValue = "NewValue2";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue2", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(2, titleNode.AssociatedData[UpdateCountKey]);
        }

        [Fact(Skip = "Review why this test is failing and refactor to match new design of dependent properties.")]
        public void TestRecursiveDependencyMixedChanges()
        {
            var container = new Types.DependentPropertyContainer { Title = "Title", Instance = new Types.SimpleObject { Name = "Test" } };
            var testContext = new TestContainerContext();
            var instanceContext = testContext.CreateInstanceContext(container);
            testContext.GraphViewModelService.AvailableUpdaters.Add(new RecursiveDependentPropertiesUpdater());
            var viewModel = instanceContext.CreateViewModel();
            var titleNode = viewModel.RootNode.GetChild(Title);
            var instanceNode = viewModel.RootNode.GetChild(Instance);

            var nameNode = viewModel.RootNode.GetChild(Instance).GetChild(Name);
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("Test", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(0, titleNode.AssociatedData[UpdateCountKey]);

            nameNode.NodeValue = "NewValue";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(1, titleNode.AssociatedData[UpdateCountKey]);

            instanceNode.NodeValue = new Types.SimpleObject { Name = "NewValue2" };
            nameNode = viewModel.RootNode.GetChild(Instance).GetChild(Name);
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue2", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(2, titleNode.AssociatedData[UpdateCountKey]);

            nameNode.NodeValue = "NewValue3";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue3", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(3, titleNode.AssociatedData[UpdateCountKey]);

            instanceNode.NodeValue = new Types.SimpleObject { Name = "NewValue4" };
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue4", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(4, titleNode.AssociatedData[UpdateCountKey]);
        }

        [Fact]
        public void TestChangeDifferentPropertyWithSameStart()
        {
            var container = new Types.DependentPropertyContainer { Title = "Title", Instance = new Types.SimpleObject { Name = "Test" } };
            var testContext = new TestContainerContext();
            var instanceContext = testContext.CreateInstanceContext(container);
            testContext.GraphViewModelService.AvailableUpdaters.Add(new SimpleDependentPropertiesUpdater());
            var viewModel = instanceContext.CreateViewModel();
            var titleNode = viewModel.RootNode.GetChild(Title);
            var nameNode = viewModel.RootNode.GetChild(Instance).GetChild(Name);
            var namNode = viewModel.RootNode.GetChild(Instance).GetChild(Nam);

            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("Test", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(0, titleNode.AssociatedData[UpdateCountKey]);

            namNode.NodeValue = "NewValue";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("Test", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(0, titleNode.AssociatedData[UpdateCountKey]);

            nameNode.NodeValue = "NewValue2";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue2", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(1, titleNode.AssociatedData[UpdateCountKey]);

            namNode.NodeValue = "NewValue3";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue2", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(1, titleNode.AssociatedData[UpdateCountKey]);

            nameNode.NodeValue = "NewValue4";
            Assert.True(titleNode.AssociatedData.ContainsKey(TestDataKey));
            Assert.Equal("NewValue4", titleNode.AssociatedData[TestDataKey]);
            Assert.Equal(2, titleNode.AssociatedData[UpdateCountKey]);
        }
    }
}
