using System.Drawing.Drawing2D;
using MineTray.Models;
using MineTray.Services;

namespace MineTray.Forms
{
    /// <summary>
    /// プレイヤーリストを表示するフォーム。
    /// </summary>
    public class PlayerListForm : Form
    {
#nullable disable
        private List<PlayerHistoryItem> _players;
        private readonly SkinManager _skinManager;
        private ListBox _listBox;
#nullable restore
        private readonly Dictionary<string, Image> _skinCache = new();
        private const int ItemHeight = 40;
        private const int MaxListHeight = 400;

        public PlayerListForm(List<PlayerHistoryItem> players, SkinManager skinManager)
        {
            _players = players;
            _skinManager = skinManager;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.Width = 250;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.White;

            _listBox = new ListBox();
            _listBox.Dock = DockStyle.Fill;
            _listBox.BackColor = Color.White;
            _listBox.ForeColor = Color.Black;
            _listBox.BorderStyle = BorderStyle.FixedSingle; // 細いボーダー
            _listBox.DrawMode = DrawMode.OwnerDrawFixed;
            _listBox.ItemHeight = ItemHeight;
            _listBox.DrawItem += ListBox_DrawItem;
            
            // フォーカス四角形を防止
            _listBox.SelectedIndexChanged += (s, e) => _listBox.ClearSelected();

            this.Controls.Add(_listBox);

            this.Deactivate += (s, e) => this.Hide();
        }

        public void SetDataSource(List<PlayerHistoryItem> players)
        {
            _players = players;
            UpdateList();
        }

        public void ShowMessage(string message)
        {
            _listBox.Items.Clear();
            _listBox.Items.Add(message);
            this.Height = ItemHeight + 4;
            _listBox.Invalidate();
        }

        public void UpdateList()
        {
            _listBox.BeginUpdate();
            _listBox.Items.Clear();
            
            // ソート: オンラインが先 (A-Z)、その後オフライン (A-Z)
            var sorted = _players
                .OrderByDescending(p => p.IsOnline)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            foreach (var p in sorted)
            {
                _listBox.Items.Add(p);
                if (!_skinCache.ContainsKey(p.Id))
                {
                    LoadSkinAsync(p.Id);
                }
            }
            
            _listBox.EndUpdate();

            // フォームのリサイズ
            int count = _listBox.Items.Count;
            int newHeight = (count * ItemHeight) + 4; // + パディング
            if (newHeight > MaxListHeight) newHeight = MaxListHeight;
            if (newHeight < 60) newHeight = 60;

            this.Height = newHeight;
        }
        
        private async void LoadSkinAsync(string uuid)
        {
            if (_skinCache.ContainsKey(uuid)) return;
            
            try 
            {
                var img = await _skinManager.GetSkinImageAsync(uuid);
                if (img != null)
                {
                    if (_listBox.IsDisposed) 
                    {
                        img.Dispose();
                        return;
                    }

                    if (!_skinCache.ContainsKey(uuid))
                    {
                        _skinCache[uuid] = img;
                        _listBox.Invalidate(); 
                    }
                    else
                    {
                        img.Dispose();
                    }
                }
            }
            catch { /* 非同期読み込みエラーを無視 */ }
        }

        private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listBox.Items.Count) return;

            e.DrawBackground();
            
            var item = _listBox.Items[e.Index];
            
            // テキストメッセージ
            if (item is string msg)
            {
                using var brush = new SolidBrush(Color.DimGray);
                using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(msg, new Font("Segoe UI", 9, FontStyle.Italic), brush, e.Bounds, format);
                return;
            }

            if (item is PlayerHistoryItem p)
            {
                // アイコンを描画
                if (_skinCache.TryGetValue(p.Id, out var img))
                {
                    e.Graphics.DrawImage(img, e.Bounds.X + 4, e.Bounds.Y + 4, 32, 32);
                }
                else
                {
                    // プレースホルダー
                    using var brush = new SolidBrush(Color.LightGray);
                    e.Graphics.FillRectangle(brush, e.Bounds.X + 4, e.Bounds.Y + 4, 32, 32);
                }

                // 名前を描画
                using var textBrush = new SolidBrush(p.IsOnline ? Color.Black : Color.Gray);
                var font = p.IsOnline ? new Font("Segoe UI", 10, FontStyle.Bold) : new Font("Segoe UI", 10, FontStyle.Regular);
                
                e.Graphics.DrawString(p.Name, font, textBrush, e.Bounds.X + 42, e.Bounds.Y + 10);
                
                // ステータスインジケーターを描画（緑の点）
                if (p.IsOnline)
                {
                    using var onlineBrush = new SolidBrush(Color.LimeGreen);
                    e.Graphics.FillEllipse(onlineBrush, e.Bounds.Right - 15, e.Bounds.Top + 16, 8, 8);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach(var img in _skinCache.Values) img.Dispose();
                _skinCache.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
