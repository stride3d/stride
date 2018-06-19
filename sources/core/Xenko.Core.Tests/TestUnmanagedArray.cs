// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xenko.Core.Tests
{
    [TestFixture]
    public class TestUnmanagedArray
    {
        [Test]
        public void Base()
        {
            using (var testing = new UnmanagedArray<float>(128))
            {
                for (var i = 0; i < testing.Length; i++)
                {
                    testing[i] = i;
                }
                for (var i = 0; i < testing.Length; i++)
                {
                    Assert.That(testing[i], Is.EqualTo(i));
                    testing[i] = -1.0f;
                }

                var managedArray = new float[128];
                for (var i = 0; i < testing.Length; i++)
                {
                    managedArray[i] = i;
                }
                testing.Write(managedArray);
                var managedArray2 = new float[128];
                testing.Read(managedArray2);
                for (var i = 0; i < testing.Length; i++)
                {
                    Assert.That(testing[i], Is.EqualTo(i));
                }
            }
        }
    }
}
