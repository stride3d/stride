using Stride.Graphics.Regression;
using Xunit;

namespace Stride.Engine.Tests;

public class PrefabTest : GameTestBase
{
    [Fact]
    public void InstantiationWithUniqueIdsTest()
    {
        PerformTest(game =>
        {
            var prefab = new Prefab();
            var entity = new Entity();
            prefab.Entities.Add(entity);

            var entities = prefab.Instantiate();

            Assert.NotEqual(entity.Id, entities[0].Id);
            Assert.NotEqual(entity.Transform.Id, entities[0].Transform.Id);
        });
    }

    [Fact]
    public void InstantiationOfEntityHierarchyWithUniqueIdsTest()
    {
        PerformTest(game =>
        {
            var prefab = new Prefab();
            var entity = new Entity();
            var child = new Entity();
            entity.AddChild(child);
            prefab.Entities.Add(entity);

            var entities = prefab.Instantiate();

            Assert.NotEqual(entity.Id, entities[0].Id);
            Assert.NotEqual(entity.Transform.Id, entities[0].Transform.Id);
            Assert.NotEqual(child.Id, entities[0].GetChild(0).Id);
            Assert.NotEqual(child.Transform.Id, entities[0].GetChild(0).Transform.Id);
        });
    }
}
