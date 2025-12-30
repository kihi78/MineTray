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
            // 高DPI設定やデフォルトフォントなどのアプリケーション構成をカスタマイズするには、
            // https://aka.ms/applicationconfiguration を参照してください。
            ApplicationConfiguration.Initialize();
            Application.Run(new MineTrayApplicationContext());
        }
    }
}