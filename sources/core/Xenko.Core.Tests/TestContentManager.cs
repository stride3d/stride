// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Xenko.Core.IO;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Serialization.Serializers;
using Xenko.Core.Storage;

namespace Xenko.Core.Tests
{
    [TestFixture]
    [ReferenceSerializer]
    [DataSerializerGlobal(typeof(ReferenceSerializer<A>), Profile = "Content")]
    [DataSerializerGlobal(typeof(ReferenceSerializer<B>), Profile = "Content")]
    [DataSerializerGlobal(typeof(ReferenceSerializer<C>), Profile = "Content")]
    [DataSerializerGlobal(typeof(ReferenceSerializer<D>), Profile = "Content")]
    public class TestContentManager
    {
        [ContentSerializer(typeof(DataContentSerializer<A>))]
        [DataContract]
        public class A
        {
            public int I;
        }

        [ContentSerializer(typeof(DataContentSerializer<B>))]
        [DataContract]
        public class B
        {
            public A A;
            public int I;
        }

        [ContentSerializer(typeof(DataContentSerializer<C>))]
        [DataContract]
        public class C : ComponentBase, IContentData
        {
            public int I { get; set; }

            public C Child { get; set; }

            public D Child2 { get; set; }

            public string Url { get; set; }
        }

        [ContentSerializer(typeof(D.Serializer))]
        [DataContract]
        public class D : ComponentBase
        {
            public D(int a)
            {
            }

            class Serializer : ContentSerializerBase<D>
            {
                public override void Serialize(ContentSerializerContext context, SerializationStream stream, D obj)
                {
                    if (context.Mode == ArchiveMode.Deserialize)
                    {
                        obj = new D(12);
                    }
                }
            }
        }

        [TestFixtureSetUp]
        public void SetupDatabase()
        {
            VirtualFileSystem.CreateDirectory(VirtualFileSystem.ApplicationDatabasePath);
            var databaseFileProvider = new DatabaseFileProvider(ContentIndexMap.NewTool(VirtualFileSystem.ApplicationDatabaseIndexName), new ObjectDatabase(VirtualFileSystem.ApplicationDatabasePath, VirtualFileSystem.ApplicationDatabaseIndexName));
            ContentManager.GetFileProvider = () => databaseFileProvider;
        }

        [Test]
        public void Simple()
        {
            var a1 = new A { I = 18 };

            var assetManager1 = new ContentManager();
            var assetManager2 = new ContentManager();

            assetManager1.Save("test", a1);

            // Use same asset manager
            var a2 = assetManager1.Load<A>("test");

            Assert.That(a2, Is.EqualTo(a1));

            // Use new asset manager
            var a3 = assetManager2.Load<A>("test");

            Assert.That(a3, Is.Not.EqualTo(a1));
            Assert.That(a3.I, Is.EqualTo(a1.I));
        }

        [Test]
        public void SimpleWithContentReference()
        {
            var b1 = new B();
            b1.A = new A { I = 18 };

            var assetManager1 = new ContentManager();
            var assetManager2 = new ContentManager();

            assetManager1.Save("test", b1);

            // Use new asset manager
            var b2 = assetManager2.Load<B>("test");

            Assert.That(b2, Is.Not.EqualTo(b1));
            Assert.That(b2.A.I, Is.EqualTo(b1.A.I));
        }

        [Test]
        public void SimpleWithContentReferenceShared()
        {
            var b1 = new B { I = 12 };
            b1.A = new A { I = 18 };
            var b2 = new B { I = 13, A = b1.A };

            var assetManager1 = new ContentManager();
            var assetManager2 = new ContentManager();

            assetManager1.Save("b1", b1);
            assetManager1.Save("b2", b2);

            // Use new asset manager
            var b1Loaded = assetManager2.Load<B>("b1");
            var b2Loaded = assetManager2.Load<B>("b2");

            Assert.That(b2Loaded, Is.Not.EqualTo(b1Loaded));
            Assert.That(b2Loaded.A, Is.EqualTo(b1Loaded.A));
        }

        [Test]
        public void SimpleLoadData()
        {
            var b1 = new B();
            b1.A = new A { I = 18 };

            var assetManager1 = new ContentManager();
            var assetManager2 = new ContentManager();
            var assetManager3 = new ContentManager();

            assetManager1.Save("test", b1);

            // Use new asset manager
            var b2 = assetManager2.Load<B>("test");

            Assert.That(b2, Is.Not.EqualTo(b1));
            Assert.That(b2.A.I, Is.EqualTo(b1.A.I));

            // Try to load without references
            var b3 = assetManager3.Load<B>("test", new ContentManagerLoaderSettings { LoadContentReferences = false });

            Assert.That(b3, Is.Not.EqualTo(b1));

            // b3.A should be null
            Assert.That(b3.A, Is.Null);
        }

        [Test]
        public void SimpleSaveData()
        {
            var b1 = new B();
            b1.A = new A { I = 18 };

            var assetManager1 = new ContentManager();
            var assetManager2 = new ContentManager();

            assetManager1.Save("test", b1);

            Assert.That(AttachedReferenceManager.GetUrl(b1.A), Is.Not.Null);
            
            var b2 = new B();
            b2.A = new A();
            var attachedReference = AttachedReferenceManager.GetOrCreateAttachedReference(b2.A);
            attachedReference.Url = AttachedReferenceManager.GetUrl(b1.A);
            attachedReference.IsProxy = true;
            assetManager1.Save("test2", b2);
            
            var b3 = assetManager2.Load<B>("test2");
            Assert.That(b3.A.I, Is.EqualTo(b1.A.I));
        }

        [Test]
        public void LifetimeShared()
        {
            var c1 = new C { I = 16 };
            var c2 = new C { I = 18 };
            c1.Child = new C { I = 32 };
            c2.Child = c1.Child;

            AttachedReferenceManager.SetUrl(c1.Child, "cchild");

            var assetManager1 = new ContentManager();
            var assetManager2 = new ContentManager();

            assetManager1.Save("c1", c1);
            assetManager1.Save("c2", c2);

            var c1Copy = assetManager2.Load<C>("c1");
            var c2Copy = assetManager2.Load<C>("c2");
            var c1ChildCopy = assetManager2.Load<C>("cchild");

            assetManager2.Unload(c1Copy);

            // Check that everything is properly unloaded
            Assert.That(((IReferencable)c1Copy).ReferenceCount, Is.EqualTo(0));
            Assert.That(((IReferencable)c2Copy).ReferenceCount, Is.EqualTo(1));
            Assert.That(((IReferencable)c1ChildCopy).ReferenceCount, Is.EqualTo(1));

            assetManager2.Unload(c2Copy);

            // Check that everything is properly unloaded
            Assert.That(((IReferencable)c2Copy).ReferenceCount, Is.EqualTo(0));
            Assert.That(((IReferencable)c1ChildCopy).ReferenceCount, Is.EqualTo(1));

            assetManager2.Unload(c1ChildCopy);

            // Check that everything is properly unloaded
            Assert.That(((IReferencable)c1ChildCopy).ReferenceCount, Is.EqualTo(0));
        }

        [Test, Ignore("Need check")]
        public void LifetimeNoSimpleConstructor()
        {
            var c1 = new C { I = 18 };
            c1.Child2 = new D(18);

            var assetManager1 = new ContentManager();
            var assetManager2 = new ContentManager();

            assetManager1.Save("c1", c1);

            var c1Copy = assetManager2.Load<C>("c1");
            Assert.That(((IReferencable)c1Copy).ReferenceCount, Is.EqualTo(1));
            Assert.That(((IReferencable)c1Copy.Child2).ReferenceCount, Is.EqualTo(1));

            assetManager2.Unload(c1Copy);
            Assert.That(((IReferencable)c1Copy).ReferenceCount, Is.EqualTo(0));
            Assert.That(((IReferencable)c1Copy.Child2).ReferenceCount, Is.EqualTo(0));
        }

        [Test]
        public void LifetimeCycles()
        {
            var c1 = new C { I = 18 };
            var c2 = new C { I = 20 };
            c1.Child = c2;
            c2.Child = c1;

            var assetManager1 = new ContentManager();
            var assetManager2 = new ContentManager();

            assetManager1.Save("c1", c1);

            var c1Copy = assetManager2.Load<C>("c1");
            Assert.That(((IReferencable)c1Copy).ReferenceCount, Is.EqualTo(1));
            Assert.That(((IReferencable)c1Copy.Child).ReferenceCount, Is.EqualTo(1));

            assetManager2.Unload(c1Copy);
            Assert.That(((IReferencable)c1Copy).ReferenceCount, Is.EqualTo(0));
            Assert.That(((IReferencable)c1Copy.Child).ReferenceCount, Is.EqualTo(0));
        }
    }
}
