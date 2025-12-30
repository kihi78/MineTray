using MineTray.Models;

namespace MineTray.Tests
{
    /// <summary>
    /// MinecraftDescriptionクラスのテスト。
    /// </summary>
    public class MinecraftDescriptionTests
    {
        [Fact]
        public void ToCleanText_WithSimpleText_ShouldReturnText()
        {
            // Arrange
            var desc = new MinecraftDescription { Text = "Hello World" };

            // Act
            var result = desc.ToCleanText();

            // Assert
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void ToCleanText_WithFormattedText_ShouldStripFormatting()
        {
            // Arrange
            var desc = new MinecraftDescription { Text = "§aWelcome §bto §cServer" };

            // Act
            var result = desc.ToCleanText();

            // Assert
            Assert.Equal("Welcome to Server", result);
        }

        [Fact]
        public void ToCleanText_WithExtra_ShouldConcatenate()
        {
            // Arrange
            var desc = new MinecraftDescription 
            { 
                Text = "Hello",
                Extra = new List<MinecraftDescription>
                {
                    new MinecraftDescription { Text = " " },
                    new MinecraftDescription { Text = "World" }
                }
            };

            // Act
            var result = desc.ToCleanText();

            // Assert
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void ToString_ShouldCallToCleanText()
        {
            // Arrange
            var desc = new MinecraftDescription { Text = "Test" };

            // Act
            var result = desc.ToString();

            // Assert
            Assert.Equal("Test", result);
        }
    }
}
