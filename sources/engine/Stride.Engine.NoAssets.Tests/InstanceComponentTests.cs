// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Stride.Core;
using Xunit;

namespace Stride.Engine.Tests
{
    public class InstanceComponentTests
    {
        [Fact]
        public void RemovedInstanceDoesNotReconnectWhenMasterChanges()
        {
            var entityManager = new TestEntityManager(new ServiceRegistry());
            var master = new InstancingComponent();
            var entityTransform = new InstancingEntityTransform();
            master.Type = entityTransform;

            var entity = new Entity { new InstanceComponent { Master = master } };
            entityManager.Add(entity);
            Assert.Same(entity.Get<InstanceComponent>(), entityTransform.GetInstanceAt(0));

            entityManager.Remove(entity);

            var replacement = new InstancingEntityTransform();
            master.Type = new InstancingUserArray();
            master.Type = replacement;
            replacement.Update();

            Assert.Equal(0, replacement.InstanceCount);
        }

        [Fact]
        public void RemovedInstanceDoesNotConnectToANewMaster()
        {
            var entityManager = new TestEntityManager(new ServiceRegistry());
            var master = new InstancingComponent();
            master.Type = new InstancingEntityTransform();
            var instance = new InstanceComponent { Master = master };
            var entity = new Entity { instance };

            entityManager.Add(entity);
            entityManager.Remove(entity);

            var replacement = new InstancingComponent();
            var replacementTransform = new InstancingEntityTransform();
            replacement.Type = replacementTransform;
            instance.Master = replacement;
            replacementTransform.Update();

            Assert.Equal(0, replacementTransform.InstanceCount);
        }

        [Fact]
        public void ReaddedInstanceResubscribesToMasterChanges()
        {
            var entityManager = new TestEntityManager(new ServiceRegistry());
            var master = new InstancingComponent();
            var entityTransform = new InstancingEntityTransform();
            master.Type = entityTransform;

            var entity = new Entity { new InstanceComponent { Master = master } };
            entityManager.Add(entity);
            entityManager.Remove(entity);
            entityManager.Add(entity);

            master.Type = new InstancingUserArray();
            entityTransform.Update();
            Assert.Equal(0, entityTransform.InstanceCount);

            var replacement = new InstancingEntityTransform();
            master.Type = replacement;
            replacement.Update();

            Assert.Equal(1, replacement.InstanceCount);
        }

        [Fact]
        public void RemovedInstanceDoesNotKeepItsEntityAlive()
        {
            var entityManager = new TestEntityManager(new ServiceRegistry());
            var master = new InstancingComponent();
            var weakEntity = CreateAndRemoveInstance(entityManager, master);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakEntity.TryGetTarget(out _));
            GC.KeepAlive(master);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference<Entity> CreateAndRemoveInstance(TestEntityManager entityManager, InstancingComponent master)
        {
            var entity = new Entity { new InstanceComponent { Master = master } };
            entityManager.Add(entity);
            entityManager.Remove(entity);

            return new WeakReference<Entity>(entity);
        }

        private sealed class TestEntityManager : EntityManager
        {
            public TestEntityManager(IServiceRegistry registry) : base(registry)
            {
            }
        }
    }
}
