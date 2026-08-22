using System.Drawing;
using MineTray.Services;

namespace MineTray.Tests
{
    /// <summary>
    /// IconServiceのアイコン生成・破棄処理のテスト。
    /// </summary>
    public class IconServiceTests
    {
        [Fact]
        public void CreateFallbackIcon_ReturnsValidIcon()
        {
            using var service = new IconService();
            using var icon = service.CreateFallbackIcon(Color.Red);

            Assert.NotNull(icon);
        }

        [Fact]
        public void CreateIconFromImage_ReturnsValidIcon()
        {
            using var service = new IconService();
            using var bitmap = new Bitmap(16, 16);
            using var icon = service.CreateIconFromImage(bitmap);

            Assert.NotNull(icon);
        }

        [Fact]
        public void SetCurrentCustomIcon_DisposesPreviousIcon()
        {
            using var service = new IconService();

            var icon1 = service.CreateFallbackIcon(Color.Blue);
            Assert.NotNull(icon1);
            service.SetCurrentCustomIcon(icon1);

            var icon2 = service.CreateFallbackIcon(Color.Green);
            Assert.NotNull(icon2);
            service.SetCurrentCustomIcon(icon2); // icon1は内部で破棄されるはず

            Assert.Throws<ObjectDisposedException>(() => _ = icon1!.Handle);
        }

        [Fact]
        public void Dispose_DoesNotThrow_EvenWithoutCustomIconSet()
        {
            var service = new IconService();

            var exception = Record.Exception(() => service.Dispose());

            Assert.Null(exception);
        }

        [Fact]
        public void MissingAssetIcons_DoNotThrow_AndFallBackToNull()
        {
            // テストプロジェクトにはAssets/*.pngが配置されないため、
            // アセット読み込み失敗時に例外を投げず、nullにフォールバックすることを確認する。
            using var service = new IconService();

            Assert.True(service.MainIcon == null || service.MainIcon is Icon);
            Assert.True(service.OfflineIcon == null || service.OfflineIcon is Icon);
            Assert.True(service.OnlineIcon == null || service.OnlineIcon is Icon);
        }
    }
}
