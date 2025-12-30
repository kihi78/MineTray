using System.Text.Json.Serialization;

namespace MineTray.Models
{
    /// <summary>
    /// Minecraftサーバーの SLP (Server List Ping) レスポンス。
    /// </summary>
    public class MinecraftServerStatus
    {
        /// <summary>
        /// サーバーバージョン情報。
        /// </summary>
        [JsonPropertyName("version")]
        public VersionInfo? Version { get; set; }

        /// <summary>
        /// プレイヤー情報。
        /// </summary>
        [JsonPropertyName("players")]
        public PlayersInfo? Players { get; set; }

        /// <summary>
        /// サーバー説明 (MOTD)。
        /// </summary>
        [JsonPropertyName("description")]
        [JsonConverter(typeof(MinecraftDescriptionConverter))]
        public MinecraftDescription? Description { get; set; }

        /// <summary>
        /// サーバーアイコン (Base64)。
        /// </summary>
        [JsonPropertyName("favicon")]
        public string? Favicon { get; set; }

        /// <summary>
        /// Mod情報 (Forgeサーバー用)。
        /// </summary>
        [JsonPropertyName("modinfo")]
        public ModInfo? ModInfo { get; set; }
    }
}
