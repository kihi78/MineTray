using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MineTray.Models
{
    /// <summary>
    /// Minecraftテキストコンポーネント（チャットメッセージ/MOTD形式）。
    /// </summary>
    [JsonConverter(typeof(MinecraftDescriptionConverter))] 
    public class MinecraftDescription
    {
        public string Text { get; set; } = "";
        public string? Translate { get; set; }
        public List<MinecraftDescription>? With { get; set; }
        public string? Color { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underlined { get; set; }
        public bool Strikethrough { get; set; }
        public bool Obfuscated { get; set; }

        public List<MinecraftDescription>? Extra { get; set; }

        public override string ToString() => ToCleanText();

        /// <summary>
        /// フォーマットを除去したクリーンテキストを返します。
        /// </summary>
        public string ToCleanText()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Text))
            {
                sb.Append(Text);
            }
            
            if (!string.IsNullOrEmpty(Translate))
            {
                sb.Append(Translate);
                if (With != null)
                {
                    sb.Append(" [");
                    foreach (var w in With)
                    {
                        sb.Append(w.ToCleanText() + ",");
                    }
                    sb.Replace(",", "]", sb.Length - 1, 1);
                }
            }

            if (Extra != null)
            {
                foreach (var extra in Extra)
                {
                    sb.Append(extra.ToCleanText());
                }
            }

            return MinecraftTextCleaner.StripFormatting(sb.ToString());
        }
    }

    /// <summary>
    /// Minecraftテキストコンポーネント用JSONコンバーター（文字列またはオブジェクト形式に対応）。
    /// </summary>
    public class MinecraftDescriptionConverter : JsonConverter<MinecraftDescription>
    {
        public override MinecraftDescription? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return new MinecraftDescription { Text = reader.GetString() ?? "" };
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    var root = doc.RootElement;
                    var desc = new MinecraftDescription();

                    if (root.TryGetProperty("text", out var prop)) desc.Text = prop.GetString() ?? "";
                    if (root.TryGetProperty("translate", out prop)) desc.Translate = prop.GetString();
                    if (root.TryGetProperty("color", out prop)) desc.Color = prop.GetString();
                    if (root.TryGetProperty("bold", out prop)) desc.Bold = prop.GetBoolean();
                    if (root.TryGetProperty("italic", out prop)) desc.Italic = prop.GetBoolean();
                    if (root.TryGetProperty("underlined", out prop)) desc.Underlined = prop.GetBoolean();
                    if (root.TryGetProperty("strikethrough", out prop)) desc.Strikethrough = prop.GetBoolean();
                    if (root.TryGetProperty("obfuscated", out prop)) desc.Obfuscated = prop.GetBoolean();

                    if (root.TryGetProperty("with", out var withProp) && withProp.ValueKind == JsonValueKind.Array)
                    {
                        desc.With = new List<MinecraftDescription>();
                        foreach (var item in withProp.EnumerateArray())
                        {
                            try
                            {
                                var argDesc = JsonSerializer.Deserialize<MinecraftDescription>(item.GetRawText(), options);
                                if (argDesc != null) desc.With.Add(argDesc);
                            }
                            catch
                            {
                                if (item.ValueKind == JsonValueKind.String)
                                    desc.With.Add(new MinecraftDescription { Text = item.GetString() ?? "" });
                                else if (item.ValueKind == JsonValueKind.Number)
                                     desc.With.Add(new MinecraftDescription { Text = item.ToString() });
                            }
                        }
                    }

                    if (root.TryGetProperty("extra", out var extraProp) && extraProp.ValueKind == JsonValueKind.Array)
                    {
                        desc.Extra = new List<MinecraftDescription>();
                        foreach (var item in extraProp.EnumerateArray())
                        {
                            var extraDesc = JsonSerializer.Deserialize<MinecraftDescription>(item.GetRawText(), options);
                            if (extraDesc != null)
                            {
                                desc.Extra.Add(extraDesc);
                            }
                        }
                    }
                    return desc;
                }
            }
            return new MinecraftDescription();
        }

        public override void Write(Utf8JsonWriter writer, MinecraftDescription value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToCleanText());
        }
    }
}
