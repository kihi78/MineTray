using System.Text.RegularExpressions;

namespace MineTray.Models
{
    /// <summary>
    /// Minecraftテキストのフォーマットコード（§）を除去するユーティリティ。
    /// </summary>
    public static class MinecraftTextCleaner
    {
        private static readonly Regex FormattingRegex = new Regex("§[0-9a-fk-or]", RegexOptions.Compiled);

        /// <summary>
        /// テキストからMinecraftのフォーマットコードを除去します。
        /// </summary>
        public static string StripFormatting(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return FormattingRegex.Replace(input, "");
        }
    }
}
