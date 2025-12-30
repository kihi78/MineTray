using MineTray.Models;

namespace MineTray.Tests
{
    /// <summary>
    /// PlayerHistoryItemクラスのテスト。
    /// </summary>
    public class PlayerHistoryItemTests
    {
        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var item = new PlayerHistoryItem();

            // Assert
            Assert.Equal("", item.Id);
            Assert.Equal("", item.Name);
            Assert.Equal("", item.ServerAddress);
            Assert.False(item.IsOnline);
            Assert.Equal(default(DateTime), item.LastSeen);
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var now = DateTime.Now;
            var item = new PlayerHistoryItem
            {
                Id = "test-uuid",
                Name = "TestPlayer",
                ServerAddress = "mc.example.com",
                IsOnline = true,
                LastSeen = now
            };

            // Assert
            Assert.Equal("test-uuid", item.Id);
            Assert.Equal("TestPlayer", item.Name);
            Assert.Equal("mc.example.com", item.ServerAddress);
            Assert.True(item.IsOnline);
            Assert.Equal(now, item.LastSeen);
        }
    }
}
