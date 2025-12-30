using System.Text.Json;
using MineTray.Models;

namespace MineTray.Services
{
    /// <summary>
    /// プレイヤー履歴の読み込み・保存を管理するマネージャー。
    /// </summary>
    public static class PlayerHistoryManager
    {
        private const string FilePath = "players.json";

        /// <summary>
        /// プレイヤー履歴をファイルから読み込みます。
        /// </summary>
        public static List<PlayerHistoryItem> Load()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    var json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<List<PlayerHistoryItem>>(json) ?? new List<PlayerHistoryItem>();
                }
                catch (Exception ex) 
                { 
                    System.Diagnostics.Debug.WriteLine($"[PlayerHistoryManager.Load] エラー: {ex.Message}");
                }
            }
            return new List<PlayerHistoryItem>();
        }

        /// <summary>
        /// プレイヤー履歴をファイルに保存します。
        /// </summary>
        public static void Save(List<PlayerHistoryItem> history)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(history, options);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[PlayerHistoryManager.Save] エラー: {ex.Message}");
            }
        }
    }
}
