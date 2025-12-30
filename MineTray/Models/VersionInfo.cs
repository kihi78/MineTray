using System.Text.Json.Serialization;

namespace MineTray.Models
{
    /// <summary>
    /// サーバーのバージョン情報。
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// バージョン名。
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// プロトコルバージョン。
        /// </summary>
        [JsonPropertyName("protocol")]
        public int Protocol { get; set; }
    }
}
