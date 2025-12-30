using System.Text.Json;

namespace MineTray.Models
{
    /// <summary>
    /// アプリケーション設定を管理するクラス。
    /// </summary>
    public class AppSettings
    {
        private const string SettingsFilePath = "settings.json";
        
        public List<ServerConfig> Servers { get; set; } = new();
        public int RotationInterval { get; set; } = 3000;
        public int SettingsSplitterDistance { get; set; } = 0;
        public bool NotificationsEnabled { get; set; } = true;
        public bool AutoStartEnabled { get; set; } = true;
        public string Language { get; set; } = "ja";
        public int LastCustomInterval { get; set; } = 3000;


        public AppSettings()
        {
            EnsureDefaultServer();
        }
        
        /// <summary>
        /// 少なくとも1つのサーバーが存在することを保証します。
        /// </summary>
        private void EnsureDefaultServer()
        {
            if (Servers.Count == 0)
            {
                Servers.Add(CreateDefaultServer());
            }
        }
        
        /// <summary>
        /// デフォルトのサーバー構成を作成します。
        /// </summary>
        private static ServerConfig CreateDefaultServer()
        {
            return new ServerConfig 
            { 
                Address = "127.0.0.1", 
                Alias = "ローカルサーバー", 
                IsSelected = true 
            };
        }

        /// <summary>
        /// 選択中のサーバーを取得します。
        /// </summary>
        public ServerConfig GetSelectedServer()
        {
            var selected = Servers.FirstOrDefault(s => s.IsSelected);
            if (selected == null)
            {
                if (Servers.Count == 0) Servers.Add(CreateDefaultServer());
                selected = Servers[0];
                selected.IsSelected = true;
            }
            return selected;
        }

        /// <summary>
        /// 設定ファイルから設定を読み込みます。
        /// </summary>
        public static AppSettings Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        settings.Servers ??= new List<ServerConfig>();
                        settings.EnsureDefaultServer();
                        return settings;
                    }
                }
                catch (Exception ex) 
                { 
                    System.Diagnostics.Debug.WriteLine($"[AppSettings.Load] エラー: {ex.Message}");
                }
            }
            return new AppSettings();
        }

        /// <summary>
        /// 設定をファイルに保存します。
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[AppSettings.Save] エラー: {ex.Message}");
            }
        }
    }
}
