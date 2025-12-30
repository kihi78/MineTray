/// <summary>
/// プレイヤー履歴アイテム。循環依存や混乱を避けるため、ここに定義。
/// </summary>
namespace MineTray.Models
{
    /// <summary>
    /// プレイヤーの履歴情報を保持するクラス。
    /// </summary>
    public class PlayerHistoryItem
    {
        /// <summary>
        /// プレイヤーのUUID。
        /// </summary>
        public string Id { get; set; } = "";
        
        /// <summary>
        /// プレイヤー名。
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// サーバーアドレス。
        /// </summary>
        public string ServerAddress { get; set; } = "";
        
        /// <summary>
        /// オンライン状態。
        /// </summary>
        public bool IsOnline { get; set; }
        
        /// <summary>
        /// 最終確認日時。
        /// </summary>
        public DateTime LastSeen { get; set; }
    }
}
