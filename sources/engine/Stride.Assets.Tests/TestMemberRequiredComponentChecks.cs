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
            public required object KeywordRequired { get; set; }
            [MemberRequired(ReportAs = MemberRequiredReportType.Error)] public required object KeywordAndAttributeRequired { get; set; }

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
                KeywordRequired = new object(),
                KeywordAndAttributeRequired = new object()
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
                KeywordRequired = new object(),
                KeywordAndAttributeRequired = new object()
            };
            var memberName = nameof(memberRequiredComponent.PublicField);
            TestSingleWarning(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingRequiredMember_PublicProp()
        {
            var memberRequiredComponent = new MemberRequiredComponent(new object(), new object())
            {
                InternalField = new object(),
                PublicProp = null,
                PublicField = new object(),
                KeywordRequired = new object(),
                KeywordAndAttributeRequired = new object()
            };
            var memberName = nameof(memberRequiredComponent.PublicProp);
            TestSingleWarning(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingRequiredMember_InitProp()
        {
            var memberRequiredComponent = new MemberRequiredComponent(null, new object())
            {
                InternalField = new object(),
                PublicProp = new object(),
                PublicField = new object(),
                KeywordRequired = new object(),
                KeywordAndAttributeRequired = new object()
            };
            var memberName = nameof(MemberRequiredComponent.InitProp);
            TestSingleWarning(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingRequiredMember_InternalProp()
        {
            var memberRequiredComponent = new MemberRequiredComponent(new object(), null)
            {
                InternalField = new object(),
                PublicProp = new object(),
                PublicField = new object(),
                KeywordRequired = new object(),
                KeywordAndAttributeRequired = new object()
            };
            var memberName = nameof(MemberRequiredComponent.InternalProp);
            TestSingleWarning(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingRequiredMember_Keyword()
        {
            var memberRequiredComponent = new MemberRequiredComponent(new object(), new object())
            {
                InternalField = new object(),
                PublicProp = new object(),
                PublicField = new object(),
                KeywordRequired = null,
                KeywordAndAttributeRequired = new object()
            };
            var memberName = nameof(MemberRequiredComponent.KeywordRequired);
            TestSingleWarning(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingRequiredMember_KeywordWithAttribute()
        {
            var memberRequiredComponent = new MemberRequiredComponent(new object(), new object())
            {
                InternalField = new object(),
                PublicProp = new object(),
                PublicField = new object(),
                KeywordRequired = new object(),
                KeywordAndAttributeRequired = null
            };
            var memberName = nameof(MemberRequiredComponent.KeywordAndAttributeRequired);
            TestSingleError(memberRequiredComponent, memberName);
        }

        [Fact]
        void EntityIsMissingAllRequiredMembers()
        {
            var memberRequiredComponent = new MemberRequiredComponent(null, null)
            {
                PublicProp = null,
                PublicField = null,
                KeywordRequired = null,
                KeywordAndAttributeRequired = null
            };
            var entity = new Entity("test");
            entity.Add(memberRequiredComponent);

            var check = new RequiredMembersCheck();
            var result = new AssetCompilerResult();

            Assert.True(check.AppliesTo(memberRequiredComponent.GetType()));
            check.Check(memberRequiredComponent, entity, null, "", result);
            Assert.Equal(7, result.Messages.Count);
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
                KeywordRequired = new object(),
                KeywordAndAttributeRequired = new object()
            };
            var memberName = nameof(memberRequiredComponent.VirtualProp);
            TestSingleWarning(memberRequiredComponent, memberName);
        }

        private static void TestSingleError(MemberRequiredComponent memberRequiredComponent, string memberName)
        {
            TestSingle(memberRequiredComponent, memberName, Core.Diagnostics.LogMessageType.Error);
        }

        private static void TestSingleWarning(MemberRequiredComponent memberRequiredComponent, string memberName)
        {
            TestSingle(memberRequiredComponent, memberName, Core.Diagnostics.LogMessageType.Warning);
        }

        private static void TestSingle(MemberRequiredComponent memberRequiredComponent, string memberName, Core.Diagnostics.LogMessageType messageType)
        {
            var entity = new Entity("Test");
            entity.Add(memberRequiredComponent);

            var check = new RequiredMembersCheck();
            var result = new AssetCompilerResult();

            Assert.True(check.AppliesTo(memberRequiredComponent.GetType()));
            check.Check(memberRequiredComponent, entity, null, "", result);
            Assert.Collection(result.Messages, (msg) =>
            {
                Assert.Equal(messageType, msg.Type);
                Assert.Contains(memberName, msg.Text);
            });
        }
    }
}
