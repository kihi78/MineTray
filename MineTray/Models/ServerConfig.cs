namespace MineTray.Models
{
    /// <summary>
    /// サーバー設定を保持するクラス。
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// サーバーアドレス。
        /// </summary>
        public string Address { get; set; } = "";
        
        /// <summary>
        /// 表示名。
        /// </summary>
        public string Alias { get; set; } = "";
        
        /// <summary>
        /// 監視対象として選択されているかどうか。
        /// </summary>
        public bool IsSelected { get; set; }
        
        public override string ToString()
        {
            return IsSelected ? "★ " + Alias : Alias;
        }
    }
}
