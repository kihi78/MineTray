using System.Text.Json.Serialization;

namespace MineTray.Models
{
    /// <summary>
    /// サーバーのプレイヤー情報。
    /// </summary>
    public class PlayersInfo
    {
        /// <summary>
        /// 最大プレイヤー数。
        /// </summary>
        [JsonPropertyName("max")]
        public int Max { get; set; }

        /// <summary>
        /// オンラインプレイヤー数。
        /// </summary>
        [JsonPropertyName("online")]
        public int Online { get; set; }

        /// <summary>
        /// プレイヤーサンプルリスト。
        /// </summary>
        [JsonPropertyName("sample")]
        public List<PlayerSample> Sample { get; set; } = new List<PlayerSample>();
    }

    /// <summary>
    /// プレイヤーサンプル情報。
    /// </summary>
    public class PlayerSample
    {
        /// <summary>
        /// プレイヤー名。
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// プレイヤーUUID。
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// フォーマットを除去したプレイヤー名。
        /// </summary>
        [JsonIgnore]
        public string CleanName => MinecraftTextCleaner.StripFormatting(Name ?? "");
    }
}
