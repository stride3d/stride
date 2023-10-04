using Stride.Assets.Entities.ComponentChecks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets.Compiler;
using Stride.Engine;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Tests the behaviour of <see cref="RequiredMembersCheck"/>.
    /// </summary>
    public class TestMemberRequiredComponentChecks
    {
        [DataContract]
        public abstract class VirtualBaseComponent : EntityComponent
        {
            [MemberRequired] public virtual object VirtualProp { get; set; }
        }

        /// <summary>
        /// Test component that has <see cref="MemberRequiredAttribute"/> on serializable members.
        /// </summary>
        [DataContract]
        public class MemberRequiredComponent : VirtualBaseComponent
        {
            // We won't test the (ReportAs = Error) case
            // it would duplicate tests and it's more important to assert
            // that presence of the attribute gives a warning
            [MemberRequired] public object PublicField;
            [DataMember][MemberRequired] internal object InternalField;
            [MemberRequired] public object PublicProp { get; set; }
            [MemberRequired]
            [DataMember] public object InitProp { get; init; }
            [MemberRequired]
            [DataMember] internal object InternalProp { get; set; }
            public override object VirtualProp { get; set; } = new object();
            public MemberRequiredComponent(object initData, object internalData)
            {
                InitProp = initData;
                InternalProp = internalData;
            }
        }

        [Fact]
        void EntityIsNotMissingRequiredMembers()
        {
            var memberRequiredComponent = new MemberRequiredComponent(new object(), new object())
            {
                InternalField = new object(),
                PublicProp = new object(),
                PublicField = new object(),
            };
            var entity = new Entity("test");
            entity.Add(memberRequiredComponent);

            var check = new RequiredMembersCheck();
            var result = new AssetCompilerResult();

            Assert.True(check.AppliesTo(memberRequiredComponent.GetType()));
            check.Check(memberRequiredComponent, entity, null, "", result);
            Assert.Empty(result.Messages);
        }

        [Fact]
        void EntityIsMissingRequiredMember_PublicField()
        {
            var memberRequiredComponent = new MemberRequiredComponent(new object(), new object())
            {
                InternalField = new object(),
                PublicProp = new object(),
                PublicField = null,
            };
            var memberName = nameof(memberRequiredComponent.PublicField);
            TestSingle(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingRequiredMember_PublicProp()
        {
            var memberRequiredComponent = new MemberRequiredComponent(new object(), new object())
            {
                InternalField = new object(),
                PublicProp = null,
                PublicField = new object(),
            };
            var memberName = nameof(memberRequiredComponent.PublicProp);
            TestSingle(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingRequiredMember_InitProp()
        {
            var memberRequiredComponent = new MemberRequiredComponent(null, new object())
            {
                InternalField = new object(),
                PublicProp = new object(),
                PublicField = new object(),
            };
            var memberName = nameof(MemberRequiredComponent.InitProp);
            TestSingle(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingRequiredMember_InternalProp()
        {
            var memberRequiredComponent = new MemberRequiredComponent(new object(), null)
            {
                InternalField = new object(),
                PublicProp = new object(),
                PublicField = new object(),
            };
            var memberName = nameof(MemberRequiredComponent.InternalProp);
            TestSingle(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingAllRequiredMembers()
        {
            var memberRequiredComponent = new MemberRequiredComponent(null, null)
            {
                PublicProp = null,
                PublicField = null,
            };
            var entity = new Entity("test");
            entity.Add(memberRequiredComponent);

            var check = new RequiredMembersCheck();
            var result = new AssetCompilerResult();

            Assert.True(check.AppliesTo(memberRequiredComponent.GetType()));
            check.Check(memberRequiredComponent, entity, null, "", result);
            Assert.Equal(5, result.Messages.Count);
        }

        [Fact]
        void EntityIsMissingRequiredMember_VirtualProp()
        {
            var memberRequiredComponent = new MemberRequiredComponent(new object(), new object())
            {
                InternalField = new object(),
                PublicProp = new object(),
                PublicField = new object(),
                VirtualProp = null,
            };
            var memberName = nameof(memberRequiredComponent.VirtualProp);
            TestSingle(memberRequiredComponent, memberName);
        }

        private static void TestSingle(MemberRequiredComponent memberRequiredComponent, string memberName)
        {
            var entity = new Entity("Test");
            entity.Add(memberRequiredComponent);

            var check = new RequiredMembersCheck();
            var result = new AssetCompilerResult();

            Assert.True(check.AppliesTo(memberRequiredComponent.GetType()));
            check.Check(memberRequiredComponent, entity, null, "", result);
            Assert.Collection(result.Messages, (msg) =>
            {
                Assert.Equal(Core.Diagnostics.LogMessageType.Warning, msg.Type);
                Assert.Contains(memberName, msg.Text);
            });
        }
    }
}
