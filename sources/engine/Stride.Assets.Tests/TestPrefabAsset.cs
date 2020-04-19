// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core.Assets;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core;
using Stride.Assets.Entities;
using Stride.Engine;

namespace Stride.Assets.Tests
{
    [DataContract("TestEntityComponent")]
    public sealed class TestEntityComponent : EntityComponent
    {
        public Entity EntityLink { get; set; }

        public EntityComponent EntityComponentLink { get; set; }
    }

    public class TestPrefabAsset
    {
        [Fact(Skip = "This test is obsolete, assets require a PropertyGraph to create metadata before being saved")]
        public void TestSerialization()
        {
            //var originAsset = CreateOriginAsset();

            //using (var stream = new MemoryStream())
            //{
            //    AssetFileSerializer.Save(stream, originAsset, null);

            //    stream.Position = 0;
            //    var serializedVersion = Encoding.UTF8.GetString(stream.ToArray());
            //    Console.WriteLine(serializedVersion);

            //    stream.Position = 0;
            //    var newAsset = AssetFileSerializer.Load<PrefabAsset>(stream, "Prefab.sdprefab").Asset;

            //    CheckAsset(originAsset, newAsset);
            //}
        }

        [Fact]
        public void TestClone()
        {
            var originAsset = CreateOriginAsset();
            var newAsset = AssetCloner.Clone(originAsset);
            CheckAsset(originAsset, newAsset, originAsset.Hierarchy.Parts.Select(x => x.Value.Entity.Id).ToDictionary(x => x, x => x));
        }

        [Fact]
        public void TestCloneWithNewIds()
        {
            var originAsset = CreateOriginAsset();
            Dictionary<Guid, Guid> idRemapping;
            var newAsset = AssetCloner.Clone(originAsset, AssetClonerFlags.GenerateNewIdsForIdentifiableObjects, out idRemapping);
            CheckAsset(originAsset, newAsset, idRemapping);
        }

        private static PrefabAsset CreateOriginAsset()
        {
            // Basic test of entity serialization with links between entities (entity-entity, entity-component)
            // E1
            //   | E2 + link to E1 via TestEntityComponent
            // E3
            // E4 + link to E3.Transform component via TestEntityComponent

            var originAsset = new PrefabAsset();

            {
                var entity1 = new Entity() { Name = "E1", Id = GuidGenerator.Get(200) };
                var entity2 = new Entity() { Name = "E2", Id = GuidGenerator.Get(400) }; // Use group property to make sure that it is properly serialized
                var entity3 = new Entity() { Name = "E3", Id = GuidGenerator.Get(100) };
                var entity4 = new Entity() { Name = "E4", Id = GuidGenerator.Get(300) };

                // TODO: Add script link

                entity1.Transform.Children.Add(entity2.Transform);

                // Test a link between entity1 and entity2
                entity2.Add(new TestEntityComponent() { EntityLink = entity1 });

                // Test a component link between entity4 and entity 3
                entity4.Add(new TestEntityComponent() { EntityComponentLink = entity3.Transform });

                originAsset.Hierarchy.Parts.Add(new EntityDesign(entity1));
                originAsset.Hierarchy.Parts.Add(new EntityDesign(entity2));
                originAsset.Hierarchy.Parts.Add(new EntityDesign(entity3));
                originAsset.Hierarchy.Parts.Add(new EntityDesign(entity4));

                originAsset.Hierarchy.RootParts.Add(entity1);
                originAsset.Hierarchy.RootParts.Add(entity3);
                originAsset.Hierarchy.RootParts.Add(entity4);
            }
            return originAsset;
        }

        private static void CheckAsset(PrefabAsset originAsset, PrefabAsset newAsset, Dictionary<Guid, Guid> idRemapping)
        {
            // Check that we have exactly the same root entities
            Assert.Equal(originAsset.Hierarchy.RootParts.Count, newAsset.Hierarchy.RootParts.Count);
            for (var i = 0; i < originAsset.Hierarchy.RootParts.Count;++i)
            {
                Assert.Equal(idRemapping[originAsset.Hierarchy.RootParts[i].Id], newAsset.Hierarchy.RootParts[i].Id);
            }
            Assert.Equal(originAsset.Hierarchy.Parts.Count, newAsset.Hierarchy.Parts.Count);

            foreach (var entityDesign in originAsset.Hierarchy.Parts)
            {
                var newEntityDesign = newAsset.Hierarchy.Parts[idRemapping[entityDesign.Value.Entity.Id]];
                Assert.NotNull(newEntityDesign);

                // Check properties
                Assert.Equal(entityDesign.Value.Entity.Name, newEntityDesign.Entity.Name);

                // Check that we have the same amount of components
                Assert.Equal(entityDesign.Value.Entity.Components.Count, newEntityDesign.Entity.Components.Count);

                // Check that we have the same children
                Assert.Equal(entityDesign.Value.Entity.Transform.Children.Count, newEntityDesign.Entity.Transform.Children.Count);

                for (int i = 0; i < entityDesign.Value.Entity.Transform.Children.Count; i++)
                {
                    var children = entityDesign.Value.Entity.Transform.Children[i];
                    var newChildren = newEntityDesign.Entity.Transform.Children[i];
                    // Make sure that it is the same entity id
                    Assert.Equal(idRemapping[children.Entity.Id], newChildren.Entity.Id);

                    // Make sure that we resolve to the global entity and not a copy
                    Assert.True(newAsset.Hierarchy.Parts.ContainsKey(newChildren.Entity.Id));
                    Assert.Equal(newChildren.Entity, newAsset.Hierarchy.Parts[newChildren.Entity.Id].Entity);
                }
            }

            var entity1 = originAsset.Hierarchy.Parts.First(it => it.Value.Entity.Name == "E1").Value.Entity;
            var entity2 = originAsset.Hierarchy.Parts.First(it => it.Value.Entity.Name == "E2").Value.Entity;
            var entity3 = originAsset.Hierarchy.Parts.First(it => it.Value.Entity.Name == "E3").Value.Entity;
            var entity4 = originAsset.Hierarchy.Parts.First(it => it.Value.Entity.Name == "E4").Value.Entity;

            // Check that we have exactly the same root entities
            var newEntityDesign1 = newAsset.Hierarchy.Parts[idRemapping[entity1.Id]];
            var newEntityDesign2 = newAsset.Hierarchy.Parts[idRemapping[entity2.Id]];
            var newEntityDesign3 = newAsset.Hierarchy.Parts[idRemapping[entity3.Id]];
            var newEntityDesign4 = newAsset.Hierarchy.Parts[idRemapping[entity4.Id]];
            
            // Check that Transform.Children is correctly setup
            Assert.Equal(newEntityDesign2.Entity.Transform, newEntityDesign1.Entity.Transform.Children.FirstOrDefault());

            // Test entity-entity link from E2 to E1
            {
                var component = newEntityDesign2.Entity.Get<TestEntityComponent>();
                Assert.NotNull(component);
                Assert.Equal(newEntityDesign1.Entity, component.EntityLink);
            }

            // Test entity-component link from E4 to E3
            {
                var component = newEntityDesign4.Entity.Get<TestEntityComponent>();
                Assert.NotNull(component);
                Assert.Equal(newEntityDesign3.Entity.Transform, component.EntityComponentLink);
            }
        }
    }
}
