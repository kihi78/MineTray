using MineTray.Models;

namespace MineTray.Tests
{
    /// <summary>
    /// AppSettingsクラスのテスト。
    /// </summary>
    public class AppSettingsTests
    {
        [Fact]
        public void Constructor_ShouldCreateDefaultServer()
        {
            // Arrange & Act
            var settings = new AppSettings();

            // Assert
            Assert.NotNull(settings.Servers);
            Assert.NotEmpty(settings.Servers);
            Assert.True(settings.Servers[0].IsSelected);
        }

        [Fact]
        public void GetSelectedServer_ShouldReturnSelectedServer()
        {
            // Arrange
            var settings = new AppSettings();
            settings.Servers.Add(new ServerConfig { Address = "test.com", Alias = "Test", IsSelected = true });
            settings.Servers[0].IsSelected = false;

            // Act
            var selected = settings.GetSelectedServer();

            // Assert
            Assert.Equal("test.com", selected.Address);
        }

        [Fact]
        public void GetSelectedServer_WhenNoneSelected_ShouldSelectFirst()
        {
            // Arrange
            var settings = new AppSettings();
            settings.Servers[0].IsSelected = false;

            // Act
            var selected = settings.GetSelectedServer();

            // Assert
            Assert.NotNull(selected);
            Assert.True(selected.IsSelected);
        }

        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var settings = new AppSettings();

            // Assert
            Assert.Equal(3000, settings.RotationInterval);
            Assert.True(settings.NotificationsEnabled);
            Assert.Equal("ja", settings.Language);
        }
    }
}
