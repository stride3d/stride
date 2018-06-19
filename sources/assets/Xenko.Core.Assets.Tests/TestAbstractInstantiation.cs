// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using NUnit.Framework;
using Xenko.Core.Reflection;

namespace Xenko.Core.Assets.Tests
{
    [TestFixture]
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

        [Test]
        public void TestAbstractClassInstantiation()
        {
            var instance = AbstractObjectInstantiator.CreateConcreteInstance(typeof(AbstractClass));

            // Check the type
            Assert.AreNotEqual(typeof(AbstractClass), instance.GetType());
            // Check the base type
            Assert.IsInstanceOf<AbstractClass>(instance);

            // Check read access to 'A' property
            Assert.DoesNotThrow(() => { var a = ((AbstractClass)instance).A; });
            // Check write access to 'A' property
            Assert.DoesNotThrow(() => { ((AbstractClass)instance).A = null; });
            // Check read access to 'B' property
            Assert.Throws<NotImplementedException>(() => { var b = ((AbstractClass)instance).B; });
            // Check write access to 'B' property
            Assert.Throws<NotImplementedException>(() => { ((AbstractClass)instance).B = null; });

            // Check call to 'MethodA'
            Assert.DoesNotThrow(() => { ((AbstractClass)instance).MethodA(); });
            // Check call to 'MethodB'
            Assert.Throws<NotImplementedException>(() => { ((AbstractClass)instance).MethodB(); });
        }

        [Test]
        public void TestInterfaceInstantiation()
        {
            var instance = AbstractObjectInstantiator.CreateConcreteInstance(typeof(IInterface));

            // Check the type
            Assert.AreNotEqual(typeof(IInterface), instance.GetType());
            // Check the base type
            Assert.IsInstanceOf<IInterface>(instance);

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

        [Test]
        public void TestInterfaceImplementationInstantiation()
        {
            var instance = AbstractObjectInstantiator.CreateConcreteInstance(typeof(InterfaceImpl));

            // Check the type
            Assert.AreNotEqual(typeof(InterfaceImpl), instance.GetType());
            Assert.AreNotEqual(typeof(IInterface), instance.GetType());
            // Check the base type
            Assert.IsInstanceOf<InterfaceImpl>(instance);
            // Check the base interface
            Assert.IsInstanceOf<IInterface>(instance);

            // Check read access to 'A' property
            Assert.DoesNotThrow(() => { var a = ((InterfaceImpl)instance).A; });
            // Check write access to 'A' property
            Assert.DoesNotThrow(() => { ((InterfaceImpl)instance).A = null; });
            // Check read access to 'B' property
            Assert.DoesNotThrow(() => { var b = ((IInterface)instance).B; });
            // Check write access to 'B' property
            Assert.DoesNotThrow(() => { ((IInterface)instance).B = null; });

            // Check call to 'MethodA'
            Assert.DoesNotThrow(() => { ((InterfaceImpl)instance).MethodA(); });
            // Check call to 'MethodB'
            Assert.DoesNotThrow(() => { ((IInterface)instance).MethodB(); });
        }
    }
}
