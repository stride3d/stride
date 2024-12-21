using Stride.Core.Storage;
using Xunit;

namespace Stride.Core.Tests
{
    public class ObjectIdTests
    {
        [Fact]
        public void ToString_ThenTryParse_GivesTheSameResult()
        {
            var id = ObjectId.New();
            var str = id.ToString();
            Assert.True(ObjectId.TryParse(str, out var parsed));
            Assert.Equal(id, parsed);
        }
    }
}
