using MineTray.Models;

namespace MineTray.Tests
{
    /// <summary>
    /// ServerConfigクラスのテスト。
    /// </summary>
    public class ServerConfigTests
    {
        [Fact]
        public void ToString_WhenSelected_ShouldHaveStar()
        {
            // Arrange
            var config = new ServerConfig { Alias = "TestServer", IsSelected = true };

            // Act
            var result = config.ToString();

            // Assert
            Assert.StartsWith("★", result);
            Assert.Contains("TestServer", result);
        }

        [Fact]
        public void ToString_WhenNotSelected_ShouldNotHaveStar()
        {
            // Arrange
            var config = new ServerConfig { Alias = "TestServer", IsSelected = false };

            // Act
            var result = config.ToString();

            // Assert
            Assert.DoesNotContain("★", result);
            Assert.Equal("TestServer", result);
        }

        [Fact]
        public void DefaultValues_ShouldBeEmpty()
        {
            // Arrange & Act
            var config = new ServerConfig();

            // Assert
            Assert.Equal("", config.Address);
            Assert.Equal("", config.Alias);
            Assert.False(config.IsSelected);
        }
    }
}
