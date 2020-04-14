// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Tests
{
    public class TestAbstractInstantiation
    {
        public abstract class AbstractClass
        {
            public object A { get; set; }

            public abstract object B { get; set; }

            public void MethodA() { }

            public abstract void MethodB();
        }

        public interface IInterface
        {
            object A { get; set; }

            object B { get; set; }

            void MethodA();

            void MethodB();
        }

        public abstract class InterfaceImpl : IInterface
        {
            public object A { get; set; }

            object IInterface.B { get; set; }

            public void MethodA() { }

            void IInterface.MethodB() { }
        }

        [Fact]
        public void TestAbstractClassInstantiation()
        {
            var instance = AbstractObjectInstantiator.CreateConcreteInstance(typeof(AbstractClass));

            // Check the type
            Assert.NotEqual(typeof(AbstractClass), instance.GetType());
            // Check the base type
            Assert.IsAssignableFrom<AbstractClass>(instance);

            // Check read access to 'A' property
            { var a = ((AbstractClass)instance).A; }
            // Check write access to 'A' property
            { ((AbstractClass)instance).A = null; }
            // Check read access to 'B' property
            Assert.Throws<NotImplementedException>(() => { var b = ((AbstractClass)instance).B; });
            // Check write access to 'B' property
            Assert.Throws<NotImplementedException>(() => { ((AbstractClass)instance).B = null; });

            // Check call to 'MethodA'
            { ((AbstractClass)instance).MethodA(); }
            // Check call to 'MethodB'
            Assert.Throws<NotImplementedException>(() => { ((AbstractClass)instance).MethodB(); });
        }

        [Fact]
        public void TestInterfaceInstantiation()
        {
            var instance = AbstractObjectInstantiator.CreateConcreteInstance(typeof(IInterface));

            // Check the type
            Assert.NotEqual(typeof(IInterface), instance.GetType());
            // Check the base type
            Assert.IsAssignableFrom<IInterface>(instance);

            // Check read access to 'A' property
            Assert.Throws<NotImplementedException>(() => { var a = ((IInterface)instance).A; });
            // Check write access to 'A' property
            Assert.Throws<NotImplementedException>(() => { ((IInterface)instance).A = null; });
            // Check read access to 'B' property
            Assert.Throws<NotImplementedException>(() => { var b = ((IInterface)instance).B; });
            // Check write access to 'B' property
            Assert.Throws<NotImplementedException>(() => { ((IInterface)instance).B = null; });

            // Check call to 'MethodA'
            Assert.Throws<NotImplementedException>(() => { ((IInterface)instance).MethodA(); });
            // Check call to 'MethodB'
            Assert.Throws<NotImplementedException>(() => { ((IInterface)instance).MethodB(); });
        }

        [Fact]
        public void TestInterfaceImplementationInstantiation()
        {
            var instance = AbstractObjectInstantiator.CreateConcreteInstance(typeof(InterfaceImpl));

            // Check the type
            Assert.NotEqual(typeof(InterfaceImpl), instance.GetType());
            Assert.NotEqual(typeof(IInterface), instance.GetType());
            // Check the base type
            Assert.IsAssignableFrom<InterfaceImpl>(instance);
            // Check the base interface
            Assert.IsAssignableFrom<IInterface>(instance);

            // Check read access to 'A' property
            { var a = ((InterfaceImpl)instance).A; }
            // Check write access to 'A' property
            { ((InterfaceImpl)instance).A = null; }
            // Check read access to 'B' property
            { var b = ((IInterface)instance).B; }
            // Check write access to 'B' property
            { ((IInterface)instance).B = null; }

            // Check call to 'MethodA'
            { ((InterfaceImpl)instance).MethodA(); }
            // Check call to 'MethodB'
            { ((IInterface)instance).MethodB(); }
        }
    }
}
