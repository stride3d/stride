//// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// See LICENSE.md for full license information.
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Xunit;
//using Xenko.Core.Assets.Analysis;

//namespace Xenko.Core.Assets.Tests
//{
//    /// <summary>
//    /// Test class for <see cref="AssetDependencyManager"/>.
//    /// </summary>
////    public class TestDependencyManager : TestBase
//    {
//        [Fact]
//        public void TestCircularAndRecursiveDependencies()
//        {
//            // -----------------------------------------------------------
//            // Tests circular references and dependencies query
//            // -----------------------------------------------------------
//            // 4 assets
//            // [asset1] is referencing [asset2]
//            // [asset2] is referencing [asset3]
//            // [asset3] is referencing [asset4]
//            // [asset4] is referencing [asset1]
//            // We create a [project1] with [asset1, asset2, asset3, asset4]
//            // Check direct input dependencies for [asset1]: [asset4]
//            // Check all all input dependencies for [asset1]: [asset4, asset3, asset2, asset1]
//            // Check direct output dependencies for [asset1]: [asset2]
//            // Check all all output dependencies for [asset1]: [asset2, asset3, asset4, asset1]
//            // -----------------------------------------------------------

//            var asset1 = new AssetObjectTest();
//            var asset2 = new AssetObjectTest();
//            var asset3 = new AssetObjectTest();
//            var asset4 = new AssetObjectTest();
//            var assetItem1 = new AssetItem("asset-1", asset1);
//            var assetItem2 = new AssetItem("asset-2", asset2);
//            var assetItem3 = new AssetItem("asset-3", asset3);
//            var assetItem4 = new AssetItem("asset-4", asset4);
//            asset1.Reference = new AssetReference(assetItem2.Id, assetItem2.Location);
//            asset2.Reference = new AssetReference(assetItem3.Id, assetItem3.Location);
//            asset3.Reference = new AssetReference(assetItem4.Id, assetItem4.Location);
//            asset4.Reference = new AssetReference(assetItem1.Id, assetItem1.Location);

//            var project = new Package();
//            project.Assets.Add(assetItem1);
//            project.Assets.Add(assetItem2);
//            project.Assets.Add(assetItem3);
//            project.Assets.Add(assetItem4);

//            // Create a session with this project
//            using (var session = new PackageSession(project))
//            {
//                var dependencyManager = session.DependencyManager;

//                // Check internal states
//                Assert.Equal(1, dependencyManager.Packages.Count); // only one project
//                Assert.Equal(4, dependencyManager.Dependencies.Count); // asset1, asset2, asset3, asset4
//                Assert.Equal(0, dependencyManager.AssetsWithMissingReferences.Count);
//                Assert.Equal(0, dependencyManager.MissingReferencesToParent.Count);

//                // Check direct input references
//                var dependenciesFirst = dependencyManager.ComputeDependencies(assetItem1.Id, AssetDependencySearchOptions.InOut);
//                Assert.Equal(1, dependenciesFirst.LinksIn.Count());
//                var copyItem = dependenciesFirst.LinksIn.FirstOrDefault();
//                Assert.NotNull(copyItem.Element);
//                Assert.Equal(assetItem4.Id, copyItem.Item.Id);

//                // Check direct output references
//                Assert.Equal(1, dependenciesFirst.LinksOut.Count());
//                copyItem = dependenciesFirst.LinksOut.FirstOrDefault();
//                Assert.NotNull(copyItem.Element);
//                Assert.Equal(assetItem2.Id, copyItem.Item.Id);

//                // Calculate full recursive references
//                var dependencies = dependencyManager.ComputeDependencies(assetItem1.Id);

//                // Check all input references (recursive)
//                var asset1RecursiveInputs = dependencies.LinksIn.OrderBy(item => item.Element.Location).ToList();
//                Assert.Equal(4, dependencies.LinksOut.Count());
//                Assert.Equal(assetItem1.Id, asset1RecursiveInputs[0].Item.Id);
//                Assert.Equal(assetItem2.Id, asset1RecursiveInputs[1].Item.Id);
//                Assert.Equal(assetItem3.Id, asset1RecursiveInputs[2].Item.Id);
//                Assert.Equal(assetItem4.Id, asset1RecursiveInputs[3].Item.Id);

//                // Check all output references (recursive)
//                var asset1RecursiveOutputs = dependencies.LinksOut.OrderBy(item => item.Element.Location).ToList();
//                Assert.Equal(4, asset1RecursiveOutputs.Count);
//                Assert.Equal(assetItem1.Id, asset1RecursiveInputs[0].Element.Id);
//                Assert.Equal(assetItem2.Id, asset1RecursiveInputs[1].Element.Id);
//                Assert.Equal(assetItem3.Id, asset1RecursiveInputs[2].Element.Id);
//                Assert.Equal(assetItem4.Id, asset1RecursiveInputs[3].Element.Id);
//            }
//        }

//        [Fact]
//        public void TestFullSession()
//        {
//            // -----------------------------------------------------------
//            // This is a more complex test mixing several different cases:
//            // -----------------------------------------------------------
//            // 4 assets
//            // [asset1] is referencing [asset2]
//            // [asset3] is referencing [asset4]
//            // We create a [project1] with [asset1, asset2, asset3]
//            // Start to evaluate the dependencies 
//            // Check the dependencies for this project, [asset4] is missing
//            // We create a [project2] and add it to the session
//            // We add [asset4] to the [project2]
//            // All depedencies should be fine
//            // Remove [project2] from session
//            // Check the dependencies for this project, [asset4] is missing
//            // -----------------------------------------------------------

//            var asset1 = new AssetObjectTest();
//            var asset2 = new AssetObjectTest();
//            var asset3 = new AssetObjectTest();
//            var asset4 = new AssetObjectTest();
//            var assetItem1 = new AssetItem("asset-1", asset1);
//            var assetItem2 = new AssetItem("asset-2", asset2);
//            var assetItem3 = new AssetItem("asset-3", asset3);
//            var assetItem4 = new AssetItem("asset-4", asset4);
//            asset1.Reference = new AssetReference(assetItem2.Id, assetItem2.Location);
//            asset3.Reference = new AssetReference(assetItem4.Id, assetItem4.Location);

//            var project = new Package();
//            project.Assets.Add(assetItem1);
//            project.Assets.Add(assetItem2);
//            project.Assets.Add(assetItem3);

//            // Create a session with this project
//            using (var session = new PackageSession(project))
//            {
//                var dependencyManager = session.DependencyManager;

//                // Check internal states
//                Action checkState1 = () =>
//                {
//                    Assert.Equal(1, dependencyManager.Packages.Count); // only one project
//                    Assert.Equal(3, dependencyManager.Dependencies.Count); // asset1, asset2, asset3
//                    Assert.Equal(1, dependencyManager.AssetsWithMissingReferences.Count); // asset3 => asset4
//                    Assert.Equal(1, dependencyManager.MissingReferencesToParent.Count); // asset4 => [asset3]

//                    // Check missing references for asset3 => X asset4
//                    var assetItemWithMissingReferences = dependencyManager.FindAssetsWithMissingReferences().ToList();
//                    Assert.Equal(1, assetItemWithMissingReferences.Count);
//                    Assert.Equal(assetItem3.Id, assetItemWithMissingReferences[0]);

//                    // Check missing reference
//                    var missingReferences = dependencyManager.FindMissingReferences(assetItem3.Id).ToList();
//                    Assert.Equal(1, missingReferences.Count);
//                    Assert.Equal(asset4.Id, missingReferences[0].Id);

//                    // Check references for: asset1 => asset2
//                    var referencesFromAsset1 = dependencyManager.ComputeDependencies(assetItem1.Id);
//                    Assert.Equal(1, referencesFromAsset1.LinksOut.Count());
//                    var copyItem = referencesFromAsset1.LinksOut.FirstOrDefault();
//                    Assert.NotNull(copyItem.Element);
//                    Assert.Equal(assetItem2.Id, copyItem.Element.Id);
//                };
//                checkState1();

//                {
//                    // Add new project (must be tracked by the dependency manager)
//                    var project2 = new Package();
//                    session.Packages.Add(project2);

//                    // Check internal states
//                    Assert.Equal(2, dependencyManager.Packages.Count);

//                    // Add missing asset4
//                    project2.Assets.Add(assetItem4);
//                    var assetItemWithMissingReferences = dependencyManager.FindAssetsWithMissingReferences().ToList();
//                    Assert.Equal(0, assetItemWithMissingReferences.Count);

//                    // Check internal states
//                    Assert.Equal(4, dependencyManager.Dependencies.Count); // asset1, asset2, asset3, asse4
//                    Assert.Equal(0, dependencyManager.AssetsWithMissingReferences.Count);
//                    Assert.Equal(0, dependencyManager.MissingReferencesToParent.Count);

//                    // Try to remove the project and double check
//                    session.Packages.Remove(project2);

//                    checkState1();
//                }
//            }
//        }

//        [Fact]
//        public void TestAssetChanged()
//        {
//            // -----------------------------------------------------------
//            // Case where an asset is changing is referencing
//            // -----------------------------------------------------------
//            // 2 assets [asset1, asset2]
//            // Change [asset1] referencing [asset2]
//            // Notify the session to mark asset1 dirty
//            // 
//            // -----------------------------------------------------------

//            var asset1 = new AssetObjectTest();
//            var asset2 = new AssetObjectTest();
//            var assetItem1 = new AssetItem("asset-1", asset1);
//            var assetItem2 = new AssetItem("asset-2", asset2);

//            var project = new Package();
//            project.Assets.Add(assetItem1);
//            project.Assets.Add(assetItem2);

//            // Create a session with this project
//            using (var session = new PackageSession(project))
//            {
//                var dependencyManager = session.DependencyManager;

//                // Check internal states
//                Assert.Equal(1, dependencyManager.Packages.Count); // only one project
//                Assert.Equal(2, dependencyManager.Dependencies.Count); // asset1, asset2
//                Assert.Equal(0, dependencyManager.AssetsWithMissingReferences.Count);
//                Assert.Equal(0, dependencyManager.MissingReferencesToParent.Count);

//                asset1.Reference = new AssetReference(assetItem2.Id, assetItem2.Location);

//                // Mark the asset dirty
//                assetItem1.IsDirty = true;

//                var dependencies1 = dependencyManager.ComputeDependencies(assetItem1.Id, AssetDependencySearchOptions.InOut);
//                var copyItem = dependencies1.LinksOut.FirstOrDefault();
//                Assert.NotNull(copyItem.Element);
//                Assert.Equal(assetItem2.Id, copyItem.Element.Id);

//                var dependencies2 = dependencyManager.ComputeDependencies(assetItem2.Id, AssetDependencySearchOptions.InOut);
//                copyItem = dependencies2.LinksIn.FirstOrDefault();
//                Assert.NotNull(copyItem.Element);
//                Assert.Equal(assetItem1.Id, copyItem.Element.Id);
//            }
//        }

//        [Fact]
//        public void TestMissingReferences()
//        {
//            // -----------------------------------------------------------
//            // Tests missing references
//            // -----------------------------------------------------------
//            // 3 assets
//            // [asset1] is referencing [asset2]
//            // [asset3] is referencing [asset1]
//            // Add asset1. Check dependencies
//            // Add asset2. Check dependencies
//            // Add asset3. Check dependencies
//            // Remove asset1. Check dependencies
//            // Add asset1. Check dependencies.
//            // Modify reference asset3 to asset1 with fake asset. Check dependencies
//            // Revert reference asset3 to asset1. Check dependencies
//            // -----------------------------------------------------------

//            var asset1 = new AssetObjectTest();
//            var asset2 = new AssetObjectTest();
//            var asset3 = new AssetObjectTest();
//            var assetItem1 = new AssetItem("asset-1", asset1);
//            var assetItem2 = new AssetItem("asset-2", asset2);
//            var assetItem3 = new AssetItem("asset-3", asset3);

//            asset1.Reference = new AssetReference(assetItem2.Id, assetItem2.Location);
//            asset3.Reference = new AssetReference(assetItem1.Id, assetItem1.Location);

//            var project = new Package();

//            // Create a session with this project
//            using (var session = new PackageSession(project))
//            {
//                var dependencyManager = session.DependencyManager;

//                // Add asset1
//                project.Assets.Add(assetItem1);
//                {
//                    var assets = dependencyManager.FindAssetsWithMissingReferences().ToList();
//                    Assert.Equal(1, assets.Count);
//                    Assert.Equal(asset1.Id, assets[0]);

//                    // Check dependencies on asset1
//                    var dependencySetAsset1 = dependencyManager.ComputeDependencies(assetItem1.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset1);

//                    Assert.Equal(0, dependencySetAsset1.LinksOut.Count());
//                    Assert.True(dependencySetAsset1.HasMissingDependencies);
//                    Assert.Equal(asset2.Id, dependencySetAsset1.BrokenLinksOut.First().Element.Id);
//                }

//                // Add asset2
//                project.Assets.Add(assetItem2);
//                {
//                    var assets = dependencyManager.FindAssetsWithMissingReferences().ToList();
//                    Assert.Equal(0, assets.Count);

//                    // Check dependencies on asset1
//                    var dependencySetAsset1 = dependencyManager.ComputeDependencies(assetItem1.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset1);

//                    Assert.Equal(1, dependencySetAsset1.LinksOut.Count());
//                    Assert.Equal(0, dependencySetAsset1.LinksIn.Count());
//                    Assert.Equal(asset2.Id, dependencySetAsset1.LinksOut.First().Element.Id);

//                    // Check dependencies on asset2
//                    var dependencySetAsset2 = dependencyManager.ComputeDependencies(assetItem2.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset2);

//                    Assert.Equal(0, dependencySetAsset2.LinksOut.Count());
//                    Assert.Equal(1, dependencySetAsset2.LinksIn.Count());
//                    Assert.Equal(asset1.Id, dependencySetAsset2.LinksIn.First().Element.Id);
//                }

//                // Add asset3
//                project.Assets.Add(assetItem3);
//                Action checkAllOk = () =>
//                {
//                    var assets = dependencyManager.FindAssetsWithMissingReferences().ToList();
//                    Assert.Equal(0, assets.Count);

//                    // Check dependencies on asset1
//                    var dependencySetAsset1 = dependencyManager.ComputeDependencies(assetItem1.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset1);

//                    Assert.Equal(1, dependencySetAsset1.LinksOut.Count());
//                    Assert.Equal(1, dependencySetAsset1.LinksIn.Count());
//                    Assert.Equal(asset2.Id, dependencySetAsset1.LinksOut.First().Element.Id);
//                    Assert.Equal(asset3.Id, dependencySetAsset1.LinksIn.First().Element.Id);

//                    // Check dependencies on asset2
//                    var dependencySetAsset2 = dependencyManager.ComputeDependencies(assetItem2.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset2);

//                    Assert.Equal(0, dependencySetAsset2.LinksOut.Count());
//                    Assert.Equal(1, dependencySetAsset2.LinksIn.Count());
//                    Assert.Equal(asset1.Id, dependencySetAsset2.LinksIn.First().Element.Id);

//                    // Check dependencies on asset3
//                    var dependencySetAsset3 = dependencyManager.ComputeDependencies(assetItem3.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset3);

//                    Assert.Equal(1, dependencySetAsset3.LinksOut.Count());
//                    Assert.Equal(0, dependencySetAsset3.LinksIn.Count());
//                    Assert.Equal(asset1.Id, dependencySetAsset3.LinksOut.First().Element.Id);
//                };
//                checkAllOk();

//                // Remove asset1
//                project.Assets.Remove(assetItem1);
//                {
//                    var assets = dependencyManager.FindAssetsWithMissingReferences().ToList();
//                    Assert.Equal(1, assets.Count);
//                    Assert.Equal(asset3.Id, assets[0]);

//                    // Check dependencies on asset2
//                    var dependencySetAsset2 = dependencyManager.ComputeDependencies(assetItem2.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset2);

//                    Assert.Equal(0, dependencySetAsset2.LinksOut.Count());
//                    Assert.Equal(0, dependencySetAsset2.LinksIn.Count());

//                    // Check dependencies on asset3
//                    var dependencySetAsset3 = dependencyManager.ComputeDependencies(assetItem3.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset3);

//                    Assert.Equal(0, dependencySetAsset3.LinksOut.Count());
//                    Assert.Equal(0, dependencySetAsset3.LinksIn.Count());
//                    Assert.True(dependencySetAsset3.HasMissingDependencies);
//                    Assert.Equal(asset1.Id, dependencySetAsset3.BrokenLinksOut.First().Element.Id);
//                }

//                // Add asset1
//                project.Assets.Add(assetItem1);
//                checkAllOk();

//                // Modify reference asset3 to asset1 with fake asset
//                var previousAsset3ToAsset1Reference = asset3.Reference;
//                asset3.Reference = new AssetReference(AssetId.New(), "fake");
//                assetItem3.IsDirty = true;
//                {
//                    var assets = dependencyManager.FindAssetsWithMissingReferences().ToList();
//                    Assert.Equal(1, assets.Count);
//                    Assert.Equal(asset3.Id, assets[0]);

//                    // Check dependencies on asset1
//                    var dependencySetAsset1 = dependencyManager.ComputeDependencies(assetItem1.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset1);

//                    Assert.Equal(1, dependencySetAsset1.LinksOut.Count());
//                    Assert.Equal(0, dependencySetAsset1.LinksIn.Count());
//                    Assert.Equal(asset2.Id, dependencySetAsset1.LinksOut.First().Element.Id);

//                    // Check dependencies on asset2
//                    var dependencySetAsset2 = dependencyManager.ComputeDependencies(assetItem2.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset2);

//                    Assert.Equal(0, dependencySetAsset2.LinksOut.Count());
//                    Assert.Equal(1, dependencySetAsset2.LinksIn.Count());
//                    Assert.Equal(asset1.Id, dependencySetAsset2.LinksIn.First().Element.Id);

//                    // Check dependencies on asset3
//                    var dependencySetAsset3 = dependencyManager.ComputeDependencies(assetItem3.Id, AssetDependencySearchOptions.InOut);
//                    Assert.NotNull(dependencySetAsset3);

//                    Assert.Equal(0, dependencySetAsset3.LinksOut.Count());
//                    Assert.Equal(0, dependencySetAsset3.LinksIn.Count());
//                    Assert.True(dependencySetAsset3.HasMissingDependencies);
//                    Assert.Equal(asset3.Reference.Id, dependencySetAsset3.BrokenLinksOut.First().Element.Id);
//                }

//                // Revert back reference from asset3 to asset1
//                asset3.Reference = previousAsset3ToAsset1Reference;
//                assetItem3.IsDirty = true;
//                checkAllOk();
//            }
//        }

//        /// <summary>
//        /// Tests the types of the links between elements.
//        /// </summary>
//        [Fact]
//        public void TestLinkType()
//        {
//            // -----------------------------------------------------------
//            // Add dependencies of several types and check the links
//            // -----------------------------------------------------------
//            // 7 assets
//            // A1 -- inherit from --> A0
//            // A2 -- inherit from --> A1
//            // A3 -- compose --> A1
//            // A1 -- compose --> A4
//            // A5 -- reference --> A1
//            // A1 -- reference --> A6
//            // 
//            // Expected links on A1:
//            // - In: A2(Inheritance), A3(Composition), A5(Reference)
//            // - Out: A0(Inheritance), A4(Composition), A6(Reference)
//            // - BrokenOut: 
//            //
//            // ------------------------------------
//            // Remove all items except A1 and check missing reference types
//            // -----------------------------------------------------------
//            //
//            // Expected broken out links
//            // - BrokenOut: A0(Inheritance), A4(Composition), A6(Reference)
//            //
//            // ---------------------------------------------------------

//            var project = new Package();
//            var assets = new List<AssetObjectTest>();
//            var assetItems = new List<AssetItem>();
//            for (int i = 0; i < 7; ++i)
//            {
//                assets.Add(new AssetObjectTest { Parts = { new AssetPartTestItem { Id = Guid.NewGuid() } } });
//                assetItems.Add(new AssetItem("asset-" + i, assets[i]));
//                project.Assets.Add(assetItems[i]);
//            }

//            assets[1].Archetype = new AssetReference(assetItems[0].Id, assetItems[0].Location);
//            assets[2].Archetype = new AssetReference(assetItems[1].Id, assetItems[1].Location);
//            assets[3].Parts[0].Base = new BasePart(new AssetReference(assetItems[1].Id, assetItems[1].Location), assets[1].Parts[0].Id, Guid.NewGuid());
//            assets[1].Parts[0].Base = new BasePart(new AssetReference(assetItems[4].Id, assetItems[4].Location), assets[4].Parts[0].Id, Guid.NewGuid());
//            assets[5].Reference = CreateAssetReference(assetItems[1]);
//            assets[1].Reference = CreateAssetReference(assetItems[6]);

//            // Create a session with this project
//            using (var session = new PackageSession(project))
//            {
//                var dependencyManager = session.DependencyManager;

//                var dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);

//                Assert.Equal(3, dependencies.LinksIn.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[3]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[5]).Type);

//                Assert.Equal(3, dependencies.LinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[4]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[6]).Type);
                
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());

//                var count = assets.Count;
//                for (int i = 0; i < count; i++)
//                {
//                    if (i != 1)
//                        project.Assets.Remove(assetItems[i]);
//                }

//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);

//                Assert.Equal(0, dependencies.LinksIn.Count());
//                Assert.Equal(0, dependencies.LinksOut.Count());

//                Assert.Equal(3, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetBrokenLinkOut(assetItems[0].Id).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetBrokenLinkOut(assetItems[4].Id).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetBrokenLinkOut(assetItems[6].Id).Type);
//            }
//        }

//        /// <summary>
//        /// Tests the types of the links between elements during progressive additions.
//        /// </summary>
//        [Fact]
//        public void TestLinkTypeProgressive()
//        {
//            // -----------------------------------------------------------
//            // Progressively add dependencies between elements and check the link types
//            // -----------------------------------------------------------
//            //
//            // 3 assets:
//            // A1 -- inherit from --> A0
//            // A2 -- inherit from --> A1
//            // A1 -- compose --> A0
//            // A2 -- compose --> A1
//            // A1 -- reference --> A0
//            // A2 -- reference --> A1
//            // 
//            // ---------------------------------------------------------

//            var project = new Package();
//            var assets = new List<AssetObjectTest>();
//            var assetItems = new List<AssetItem>();
//            for (int i = 0; i < 3; ++i)
//            {
//                assets.Add(new AssetObjectTest { Parts = { new AssetPartTestItem { Id = Guid.NewGuid() } } });
//                assetItems.Add(new AssetItem("asset-" + i, assets[i]));
//                project.Assets.Add(assetItems[i]);
//            }

//            // Create a session with this project
//            using (var session = new PackageSession(project))
//            {
//                var dependencyManager = session.DependencyManager;

//                var dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(0, dependencies.LinksIn.Count());
//                Assert.Equal(0, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
                
//                assets[1].Reference = CreateAssetReference(assetItems[0]);
//                assetItems[1].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(0, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[2].Reference = CreateAssetReference(assetItems[1]);
//                assetItems[2].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[1].Archetype = new AssetReference(assetItems[0].Id, assetItems[0].Location);
//                assetItems[1].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[2].Archetype = new AssetReference(assetItems[1].Id, assetItems[1].Location);
//                assetItems[2].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[1].Parts[0].Base = new BasePart(new AssetReference(assetItems[0].Id, assetItems[0].Location), assets[0].Parts[0].Id, Guid.NewGuid());
//                assetItems[1].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[2].Parts[0].Base = new BasePart(new AssetReference(assetItems[1].Id, assetItems[1].Location), assets[1].Parts[0].Id, Guid.NewGuid());
//                assetItems[2].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                project.Assets.Remove(assetItems[0]);
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(0, dependencies.LinksOut.Count());
//                Assert.Equal(1, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetBrokenLinkOut(assetItems[0].Id).Type);

//                project.Assets.Remove(assetItems[2]);
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(0, dependencies.LinksIn.Count());
//                Assert.Equal(0, dependencies.LinksOut.Count());
//                Assert.Equal(1, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetBrokenLinkOut(assetItems[0].Id).Type);

//                project.Assets.Add(assetItems[0]);
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(0, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                project.Assets.Add(assetItems[2]);
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[2].Archetype = null;
//                assetItems[2].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[1].Archetype = null;
//                assetItems[1].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[2].Parts[0].Base = null;
//                assetItems[2].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[1].Parts[0].Base = null;
//                assetItems[1].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(1, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkIn(assetItems[2]).Type);
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[2].Reference = null;
//                assetItems[2].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(0, dependencies.LinksIn.Count());
//                Assert.Equal(1, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//                Assert.Equal(ContentLinkType.Reference, dependencies.GetLinkOut(assetItems[0]).Type);

//                assets[1].Reference = null;
//                assetItems[1].IsDirty = true;
//                dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                Assert.Equal(0, dependencies.LinksIn.Count());
//                Assert.Equal(0, dependencies.LinksOut.Count());
//                Assert.Equal(0, dependencies.BrokenLinksOut.Count());
//            }
//        }

//        private AssetReference CreateAssetReference(AssetItem item)
//        {
//            return new AssetReference(item.Id, item.Location);
//        }

//        [Fact]
//        public void TestCompositionsInAndOut()
//        {
//            // -----------------------------------------------------------
//            // 3 assets
//            // a1 : two parts
//            // a2 (baseParts: a1, 2 instances -> 4 parts)
//            // a3 (base: a2)
//            // -----------------------------------------------------------

//            var package = new Package();

//            var assetItems = package.Assets;

//            var a1 = new TestAssetWithParts();
//            a1.Parts.Add(new AssetPartTestItem(Guid.NewGuid()));
//            a1.Parts.Add(new AssetPartTestItem(Guid.NewGuid()));
//            var a1Item = new AssetItem("a1", a1);
//            assetItems.Add(a1Item);

//            var a2 = new TestAssetWithParts();
//            var aPartInstance1 = (TestAssetWithParts)a1.CreateDerivedAsset("a1");
//            var aPartInstance2 = (TestAssetWithParts)a1.CreateDerivedAsset("a1");
//            a2.AddParts(aPartInstance1);
//            a2.AddParts(aPartInstance2);
//            var a2Item = new AssetItem("a2", a2);
//            assetItems.Add(a2Item);

//            var a3 = a2.CreateDerivedAsset("a2");
//            var a3Item = new AssetItem("a3", a3);
//            assetItems.Add(a3Item);

//            // Create a session with this project
//            using (var session = new PackageSession(package))
//            {
//                var dependencyManager = session.DependencyManager;

//                //var deps = dependencyManager.FindDependencySet(aPartInstance1.Parts[0].Id);
//                var deps = dependencyManager.ComputeDependencies(a2Item.Id, AssetDependencySearchOptions.InOut);
//                Assert.NotNull(deps);

//                // The dependencies is the same as the a2 dependencies
//                Assert.Equal(a2.Id, deps.Id);

//                Assert.False(deps.HasMissingDependencies);

//                Assert.Equal(1, deps.LinksIn.Count()); // a3 inherits from a2
//                Assert.Equal(1, deps.LinksOut.Count()); // a2 use composition inheritance from a1

//                var linkIn = deps.LinksIn.FirstOrDefault();
//                Assert.Equal(a3.Id, linkIn.Item.Id);
//                // a3 has a2 as archetype (Inheritance) and its parts are referencing a2 parts (CompositionInheritance)
//                Assert.Equal(ContentLinkType.Reference, linkIn.Type);

//                var linkOut = deps.LinksOut.FirstOrDefault();
//                Assert.Equal(a1.Id, linkOut.Item.Id);
//                Assert.Equal(ContentLinkType.Reference, linkOut.Type);
//            }
//        }

//        /// <summary>
//        /// Tests that the asset cached in the session's dependency manager are correctly updated when IsDirty is set to true.
//        /// </summary>
//        [Fact]
//        public void TestCachedAssetUpdate()
//        {
//            // -----------------------------------------------------------
//            // Change a property of A0 and see if the version of A0 returned by dependency computation from A1 is valid.
//            // -----------------------------------------------------------
//            // 2 assets
//            // A1 -- inherit from --> A0
//            // 
//            // -----------------------------------------------------------
            
//            var project = new Package();
//            var assets = new List<AssetObjectTest>();
//            var assetItems = new List<AssetItem>();
//            for (int i = 0; i < 2; ++i)
//            {
//                assets.Add(new AssetObjectTest());
//                assetItems.Add(new AssetItem("asset-" + i, assets[i]));
//                project.Assets.Add(assetItems[i]);
//            }

//            assets[1].Archetype = new AssetReference(assetItems[0].Id, assetItems[0].Location);

//            using (var session = new PackageSession(project))
//            {
//                var dependencyManager = session.DependencyManager;

//                assets[0].RawAsset = "tutu";
//                assetItems[0].IsDirty = true;

//                var dependencies = dependencyManager.ComputeDependencies(assetItems[1].Id);
//                var asset0 = dependencies.GetLinkOut(assetItems[0]);
//                Assert.Equal(assets[0].RawAsset, ((AssetObjectTest)asset0.Item.Asset).RawAsset);
//            }
//        }

//        //[Fact(Skip = "Need check")]
//        //public void TestTrackingPackageWithAssetsAndSave()
//        //{
//        //    var dirPath = Path.Combine(Environment.CurrentDirectory, DirectoryTestBase + @"TestTracking");
//        //    TestHelper.TryDeleteDirectory(dirPath);

//        //    string testGenerated1 = Path.Combine(dirPath, "TestTracking.xkpkg");

//        //    var project = new Package { FullPath = testGenerated1 };
//        //    project.AssetFolders.Add(new AssetFolder("."));
//        //    var asset1 = new AssetObjectTest();
//        //    var assetItem1 = new AssetItem("asset-1", asset1);
//        //    project.Assets.Add(assetItem1);

//        //    using (var session = new PackageSession(project))
//        //    {

//        //        var dependencies = session.DependencyManager;
//        //        dependencies.TrackingSleepTime = 10;
//        //        dependencies.EnableTracking = true;

//        //        // Save the session
//        //        {
//        //            var result = session.Save();
//        //            Assert.False(result.HasErrors);

//        //            // Wait enough time for events
//        //            Thread.Sleep(100);

//        //            // Make sure that save is not generating events
//        //            var events = dependencies.FindAssetFileChangedEvents().ToList();
//        //            Assert.Equal(0, events.Count);

//        //            // Check tracked directories
//        //            var directoriesTracked = dependencies.DirectoryWatcher.GetTrackedDirectories();
//        //            Assert.Equal(1, directoriesTracked.Count);
//        //            Assert.Equal(dirPath.ToLowerInvariant(), directoriesTracked[0].ToLowerInvariant());

//        //            // Simulate multiple change an asset on the disk
//        //            File.SetLastWriteTime(assetItem1.FullPath, DateTime.Now);
//        //            Thread.Sleep(100);

//        //            // Check that we are capturing this event
//        //            events = dependencies.FindAssetFileChangedEvents().ToList();
//        //            Assert.Equal(1, events.Count);
//        //            Assert.Equal(assetItem1.Location, events[0].AssetLocation);
//        //            Assert.Equal(AssetFileChangedType.Updated, events[0].ChangeType);
//        //        }

//        //        // Save the project to another location
//        //        {
//        //            var dirPath2 = Path.Combine(Environment.CurrentDirectory, DirectoryTestBase + @"TestTracking2");
//        //            TestHelper.TryDeleteDirectory(dirPath2);
//        //            string testGenerated2 = Path.Combine(dirPath2, "TestTracking.xkpkg");

//        //            project.FullPath = testGenerated2;
//        //            var result = session.Save();
//        //            Assert.False(result.HasErrors);

//        //            // Wait enough time for events
//        //            Thread.Sleep(200);

//        //            // Make sure that save is not generating events
//        //            var events = dependencies.FindAssetFileChangedEvents().ToList();
//        //            Assert.Equal(0, events.Count);

//        //            // Check tracked directories
//        //            var directoriesTracked = dependencies.DirectoryWatcher.GetTrackedDirectories();
//        //            Assert.Equal(1, directoriesTracked.Count);
//        //            Assert.Equal(dirPath2.ToLowerInvariant(), directoriesTracked[0].ToLowerInvariant());
//        //        }

//        //        // Copy file to simulate a new file on the disk (we will not try to load it as it has the same guid 
//        //        {
//        //            var fullPath = assetItem1.FullPath;
//        //            var newPath = Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + "2" + Path.GetExtension(fullPath));
//        //            File.Copy(fullPath, newPath);

//        //            // Wait enough time for events
//        //            Thread.Sleep(200);

//        //            // Make sure that save is not generating events
//        //            var events = dependencies.FindAssetFileChangedEvents().ToList();
//        //            Assert.Equal(1, events.Count);
//        //            Assert.True((events[0].ChangeType & AssetFileChangedType.Added) != 0);
//        //        }
//        //    }
//        //}
//    }
//}
