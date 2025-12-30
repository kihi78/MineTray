using System.Text.Json.Serialization;

namespace MineTray.Models
{
    /// <summary>
    /// サーバーのMod情報。
    /// </summary>
    public class ModInfo
    {
        /// <summary>
        /// Modタイプ。
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Modリスト。
        /// </summary>
        [JsonPropertyName("modList")]
        public List<ModItem>? ModList { get; set; }
    }

    /// <summary>
    /// 個別のMod情報。
    /// </summary>
    public class ModItem
    {
        /// <summary>
        /// Mod ID。
        /// </summary>
        [JsonPropertyName("modid")]
        public string? ModId { get; set; }

        /// <summary>
        /// Modバージョン。
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }
}
