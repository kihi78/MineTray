using System.Drawing;
using System.Runtime.InteropServices;

namespace MineTray.Services
{
    /// <summary>
    /// トレイアイコンの読み込み、表示、アニメーションを管理します。
    /// </summary>
    public class IconService : IDisposable
    {
        private Icon? _iconMain;
        private Icon? _iconOffline;
        private Icon? _iconOnline;
        private Icon? _currentCustomIcon;
        
        public Icon? MainIcon => _iconMain;
        public Icon? OfflineIcon => _iconOffline;
        public Icon? OnlineIcon => _iconOnline;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public IconService()
        {
            LoadIcons();
        }

        private void LoadIcons()
        {
            _iconMain = LoadIconFromAsset("icon_main.png");
            _iconOffline = LoadIconFromAsset("icon_offline.png");
            _iconOnline = LoadIconFromAsset("icon_online.png");
        }

        private Icon? LoadIconFromAsset(string filename)
        {
            try 
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", filename);
                if (File.Exists(path))
                {
                    using var bmp = new Bitmap(path);
                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IconService.LoadIconFromAsset] 読み込み失敗 ({filename}): {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// スキン画像からアイコンを作成します。
        /// </summary>
        public Icon? CreateIconFromImage(Image image)
        {
            try
            {
                using var bitmap = new Bitmap(image, new Size(32, 32));
                IntPtr hIcon = bitmap.GetHicon();
                using var tempIcon = Icon.FromHandle(hIcon);
                var clonedIcon = (Icon)tempIcon.Clone();
                DestroyIcon(hIcon);
                return clonedIcon;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IconService.CreateIconFromImage] エラー: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// シンプルな色付きフォールバックアイコンを作成します。
        /// </summary>
        public Icon? CreateFallbackIcon(Color color)
        {
            try
            {
                using var bmp = new Bitmap(32, 32);
                using var g = Graphics.FromImage(bmp);
                using var brush = new SolidBrush(color);
                g.FillEllipse(brush, 2, 2, 28, 28);
                
                IntPtr hIcon = bmp.GetHicon();
                using var tempIcon = Icon.FromHandle(hIcon);
                var clonedIcon = (Icon)tempIcon.Clone();
                DestroyIcon(hIcon);
                return clonedIcon;
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[IconService.CreateFallbackIcon] エラー: {ex.Message}"); 
            }
            return null;
        }

        /// <summary>
        /// 適切なステータスアイコンを取得します。
        /// </summary>
        public Icon GetStatusIcon(bool isOffline, bool hasPlayers, int playerCount)
        {
            if (isOffline)
            {
                return _iconOffline ?? CreateFallbackIcon(Color.Gray) ?? SystemIcons.Warning;
            }
            
            if (playerCount == 0)
            {
                return _iconOnline ?? _iconMain ?? SystemIcons.Application;
            }
            
            return _iconMain ?? SystemIcons.Application;
        }

        /// <summary>
        /// 現在のカスタムアイコンを破棄し、GDI+ハンドルリークを防ぎます。
        /// </summary>
        public void DisposeCurrentCustomIcon()
        {
            if (_currentCustomIcon != null)
            {
                _currentCustomIcon.Dispose();
                _currentCustomIcon = null;
            }
        }

        public void SetCurrentCustomIcon(Icon? icon)
        {
            DisposeCurrentCustomIcon();
            _currentCustomIcon = icon;
        }

        public void Dispose()
        {
            DisposeCurrentCustomIcon();
            _iconMain?.Dispose();
            _iconOffline?.Dispose();
            _iconOnline?.Dispose();
        }
    }
}
