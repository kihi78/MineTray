using System.Drawing;

namespace MineTray.Services
{
    /// <summary>
    /// プレイヤースキンのキャッシュ管理とダウンロードを行います。
    /// </summary>
    public class SkinManager
    {
        private const string CacheFolder = "cache";
        private const string BaseUrl = "https://minotar.net/avatar/{0}/64";
        private readonly HttpClient _httpClient;

        public SkinManager()
        {
            _httpClient = new HttpClient();
            if (!Directory.Exists(CacheFolder))
            {
                Directory.CreateDirectory(CacheFolder);
            }
        }

        /// <summary>
        /// スキン画像のパスを取得します。キャッシュにない場合はダウンロードします。
        /// </summary>
        public async Task<string?> GetSkinPathAsync(string uuid)
        {
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), CacheFolder, $"{uuid}.png");
                
                if (File.Exists(path))
                {
                    // キャッシュの有効期限をチェック
                    var info = new FileInfo(path);
                    if (DateTime.Now - info.LastWriteTime < TimeSpan.FromHours(24))
                    {
                        return path;
                    }
                }

                // ダウンロード
                string url = string.Format(BaseUrl, uuid);
                var data = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(path, data);
                return path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkinManager.GetSkinPathAsync] エラー ({uuid}): {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// スキン画像をImageオブジェクトとして取得します。
        /// </summary>
        public async Task<Image?> GetSkinImageAsync(string uuid)
        {
            var path = await GetSkinPathAsync(uuid);
            if (path != null && File.Exists(path))
            {
                try 
                {
                    // ファイルをロックせずに読み込み
                    using var ms = new MemoryStream(await File.ReadAllBytesAsync(path));
                    using var tempImage = Image.FromStream(ms);
                    return new Bitmap(tempImage);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SkinManager.GetSkinImageAsync] エラー ({uuid}): {ex.Message}");
                    try { File.Delete(path); } catch {} // 破損ファイルを削除
                    return null;
                }
            }
            return null;
        }
    }
}
