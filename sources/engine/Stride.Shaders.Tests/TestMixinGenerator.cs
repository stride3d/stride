// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using System.Linq;

using Xunit;

using Stride.Core.Serialization;
using Stride.Rendering;

namespace Stride.Shaders.Tests
{
    /// <summary>
    /// Tests for the mixins code generation and runtime API.
    /// </summary>
    public partial class TestMixinGenerator
    {
        /// <summary>
        /// Tests a simple mixin.
        /// </summary>
        [Fact]
        public void TestSimple()
        {
            var properties = new ShaderMixinParameters();

            var mixin = GenerateMixin("DefaultSimple", properties);
            mixin.CheckMixin("A", "B", "C");
        }

        /// <summary>
        /// Tests with a child mixin.
        /// </summary>
        [Fact]
        public void TestSimpleChild()
        {
            var properties = new ShaderMixinParameters();

            var mixin = GenerateMixin("DefaultSimpleChild", properties);
            mixin.CheckMixin("A", "B", "C", "C1", "C2");
        }

        /// <summary>
        /// Tests a simple composition
        /// </summary>
        [Fact]
        public void TestSimpleCompose()
        {
            var properties = new ShaderMixinParameters();

            var mixin = GenerateMixin("DefaultSimpleCompose", properties);
            mixin.CheckMixin("A", "B", "C");
            mixin.CheckComposition("x", "X");
        }

        /// <summary>
        /// Tests simgple parameters usage
        /// </summary>
        [Fact]
        public void TestSimpleParams()
        {
            var properties = new ShaderMixinParameters();

            var mixin = GenerateMixin("DefaultSimpleParams", properties);
            mixin.CheckMixin("A", "B", "D");
            mixin.CheckComposition("y", "Y");
            mixin.CheckMacro("Test", "ok");

            // Set a key to modify the mixin
            properties.Set(Test7.TestParameters.param1, true);

            mixin = GenerateMixin("DefaultSimpleParams", properties);
            mixin.CheckMixin("A", "B", "C");
            mixin.CheckComposition("x", "X");
            mixin.CheckMacro("param2", 1);
        }

        /// <summary>
        /// Tests clone.
        /// </summary>
        [Fact]
        public void TestSimpleClone()
        {
            var properties = new ShaderMixinParameters();

            var mixin = GenerateMixin("DefaultSimpleClone", properties);
            mixin.CheckMixin("A", "B", "C");

            var childMixin = GenerateMixin("DefaultSimpleClone.Test", properties);
            childMixin.CheckMixin("A", "B", "C", "C1", "C2");
        }


        /// <summary>
        /// Test parameters
        /// </summary>
        [Fact]
        public void TestMixinAndComposeKeys()
        {
            var properties = new ShaderMixinParameters();

            var subCompute1Key = TestABC.TestParameters.UseComputeColor2.ComposeWith("SubCompute1");
            var subCompute2Key = TestABC.TestParameters.UseComputeColor2.ComposeWith("SubCompute2");
            var subComputesKey = TestABC.TestParameters.UseComputeColorRedirect.ComposeWith("SubComputes[0]");

            properties.Set(subCompute1Key, true);
            properties.Set(subComputesKey, true);

            var mixin = GenerateMixin("test_mixin_compose_keys", properties);
            mixin.CheckMixin("A");

            Assert.Equal(3, mixin.Compositions.Count);

            Assert.True(mixin.Compositions.ContainsKey("SubCompute1"));
            Assert.True(mixin.Compositions.ContainsKey("SubCompute2"));
            Assert.True(mixin.Compositions.ContainsKey("SubComputes"));

            Assert.Equal("mixin TestComputeColor2", mixin.Compositions["SubCompute1"].ToString());
            Assert.Equal("mixin TestComputeColor", mixin.Compositions["SubCompute2"].ToString());
            Assert.Equal("[mixin TestComputeColorRedirect [{ColorRedirect = mixin TestComputeColor2}]]", mixin.Compositions["SubComputes"].ToString());
        }

        /// <summary>
        /// Tests the complex parameters (array and nested using)
        /// </summary>
        [Fact]
        public void TestComplexParams()
        {
            var properties = new ShaderMixinParameters();

            // Populate the the properties used by the mixin
            var subParam1 = new Test1.SubParameters();
            var subParameters = new Test1.SubParameters[4];
            for (int i = 0; i < subParameters.Length; i++)
            {
                subParameters[i] = new Test1.SubParameters();
            }

            properties.Set(Test1.TestParameters.subParam1, subParam1);
            properties.Set(Test1.TestParameters.subParameters, subParameters);

            // Generate the mixin with default properties
            var mixin = GenerateMixin("DefaultComplexParams", properties);
            mixin.CheckMixin("A", "B", "C", "D");

            // Modify properties in order to modify mixin
            for (int i = 0; i < subParameters.Length; i++)
            {
                subParameters[i].Set(Test1.SubParameters.param1, (i & 1) == 0);
            }
            subParam1.Set(Test1.SubParameters.param2, 2);

            mixin = GenerateMixin("DefaultComplexParams", properties);
            mixin.CheckMixin("A", "B", "C", "C1", "C3");
        }
    }
}
