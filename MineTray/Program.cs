namespace MineTray
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメインエントリポイント。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // スタートアップ起動時の相対パスエラー回避のため、カレントディレクトリをアプリ本体のフォルダに変更
                string? exeDir = AppContext.BaseDirectory;
                if (!string.IsNullOrEmpty(exeDir))
                {
                    Environment.CurrentDirectory = exeDir;
                }

                // 高DPI設定やデフォルトフォントなどのアプリケーション構成をカスタマイズするには、
                // https://aka.ms/applicationconfiguration を参照してください。
                ApplicationConfiguration.Initialize();
                Application.Run(new MineTrayApplicationContext());
            }
            catch (Exception ex)
            {
                // 起動失敗時のクラッシュログ出力
                try
                {
                    string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MineTray");
                    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                    
                    string logFile = Path.Combine(logDir, "startup_error.log");
                    File.AppendAllText(logFile, $"[{DateTime.Now}] 起動クラッシュ: {ex.Message}\n{ex.StackTrace}\n\n");
                }
                catch
                {
                    try
                    {
                        string tempLogFile = Path.Combine(Path.GetTempPath(), "MineTray_startup_error.log");
                        File.AppendAllText(tempLogFile, $"[{DateTime.Now}] 起動クラッシュ: {ex.Message}\n{ex.StackTrace}\n\n");
                    }
                    catch { }
                }
            }
        }
    }
}