using MineTray.Models;

namespace MineTray.Tests
{
    /// <summary>
    /// MinecraftTextCleanerクラスのテスト。
    /// </summary>
    public class MinecraftTextCleanerTests
    {
        [Theory]
        [InlineData("§aHello", "Hello")]
        [InlineData("§bTest§cText", "TestText")]
        [InlineData("§r§lBold", "Bold")]
        [InlineData("NoFormatting", "NoFormatting")]
        [InlineData("", "")]
        public void StripFormatting_ShouldRemoveMinecraftCodes(string input, string expected)
        {
            // Act
            var result = MinecraftTextCleaner.StripFormatting(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void StripFormatting_WithNull_ShouldReturnEmpty()
        {
            // Act
            var result = MinecraftTextCleaner.StripFormatting(null!);

            // Assert
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData("§0", "")]  // Black
        [InlineData("§1", "")]  // Dark Blue
        [InlineData("§f", "")]  // White
        [InlineData("§k", "")]  // Obfuscated
        [InlineData("§l", "")]  // Bold
        [InlineData("§r", "")]  // Reset
        public void StripFormatting_ShouldHandleAllCodes(string input, string expected)
        {
            // Act
            var result = MinecraftTextCleaner.StripFormatting(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
