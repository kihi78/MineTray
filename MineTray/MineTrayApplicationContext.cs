using System.Runtime.InteropServices;
using MineTray.Models;
using MineTray.Services;
using MineTray.Forms;
using Timer = System.Windows.Forms.Timer;

namespace MineTray
{

    public class MineTrayApplicationContext : ApplicationContext
    {
#nullable disable
        // コンストラクタで初期化されるフィールド - null許容を無効化
        private NotifyIcon _notifyIcon;
        
        // === 分離されたタイマー ===
        private Timer _pollTimer;       // サーバーPing用 (低速、60秒)
        private Timer _animationTimer;  // アイコンローテーション用 (高速、ユーザー定義)
        
        // === サービス (段階的移行用) ===
        private IconService _iconService;
        private NotificationService _notificationService;
        
        private MinecraftServerPinger _pinger;
        private SkinManager _skinManager;
        private AppSettings _settings;
        private List<PlayerHistoryItem> _playerHistory;
#nullable restore

        private PlayerListForm? _playerListForm;

        // 設定・データ
        
        private int _pollInterval = 60000;
        private bool _isFirstPoll = true;
        
        private MinecraftServerStatus? _lastStatus;
        
        // === アニメーション用共有データ ===
        private List<Image> _currentSkins = new();  // Pollで更新、アニメーションで参照
        private int _rotationIndex = 0;
        private Icon? _currentCustomIcon;
        
        private HashSet<string> _lastOnlineIds = new(StringComparer.OrdinalIgnoreCase);
        private string _lastServerAddress = string.Empty;
        
        // スマート参加/退出検出
        private Dictionary<string, int> _uuidLastSeenPoll = new(StringComparer.OrdinalIgnoreCase);
        private int _pollCount = 0;
        private const int LEAVE_THRESHOLD_POLLS = 5; // 5ポール = 約5分

        // カスタムアイコン
        private Icon? _iconMain;
        private Icon? _iconOffline;
        private Icon? _iconOnline;

        public static Icon? MainIcon { get; private set; } // フォームで共有

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        public MineTrayApplicationContext()
        {
            _settings = AppSettings.Load();
            _playerHistory = PlayerHistoryManager.Load();
            
            // Load Custom Icons (kept for backward compatibility)
            _iconMain = LoadIconFromAsset("icon_main.png");
            _iconOffline = LoadIconFromAsset("icon_offline.png");
            _iconOnline = LoadIconFromAsset("icon_online.png");
            
            MainIcon = _iconMain;
            
            // Initialize Services (for future gradual migration)
            _iconService = new IconService();
            _notificationService = new NotificationService();
            _notificationService.NotificationsEnabled = _settings.NotificationsEnabled;
            _notificationService.OnNotify += (title, msg) => Notify(title, msg, ToolTipIcon.Info);

            _pinger = new MinecraftServerPinger();
            _skinManager = new SkinManager();
            
            _playerListForm = new PlayerListForm(_playerHistory, _skinManager);

            InitializeTrayIcon();
            InitializeTimers();

            // 自動起動設定を適用
            SetAutoStart(_settings.AutoStartEnabled);

            // 初回ポーリング
            Task.Run(async () => 
            {
                await Task.Delay(1000);
                if (_notifyIcon != null && _notifyIcon.Visible)
                     TryPoll(); // 安全なラッパーを使用
            });
        }
        
        /// <summary>
        /// Windows起動時の自動起動を設定/解除します。
        /// </summary>
        private void SetAutoStart(bool enable)
        {
            try
            {
                const string appName = "MineTray";
                string exePath = Application.ExecutablePath;
                
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                
                if (key == null) return;
                
                if (enable)
                {
                    key.SetValue(appName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SetAutoStart] エラー: {ex.Message}");
            }
        }
        
        private Icon? LoadIconFromAsset(string filename)
        {
            try 
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", filename);
                if (File.Exists(path))
                {
                    using (var bmp = new Bitmap(path))
                    {
                        return Icon.FromHandle(bmp.GetHicon());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadIconFromAsset] 読み込み失敗 ({filename}): {ex.Message}");
            }
            return null;
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = _iconMain ?? SystemIcons.Application,
                Visible = true,
                Text = "MineTray: Initializing..."
            };

            UpdateContextMenu();
            
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                   ShowPlayerList();
                }
            };
        }

        private void UpdateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.RenderMode = ToolStripRenderMode.System; 
            bool isJa = _settings.Language != "en";
            // 設定サブメニュー
            var settingsItem = new ToolStripMenuItem(isJa ? "設定" : "Settings");
            
            // 1. サーバー設定
            var serverSettingsItem = new ToolStripMenuItem(isJa ? "サーバー設定" : "Server Settings");
            serverSettingsItem.Click += (s, e) => ShowSettings();
            settingsItem.DropDownItems.Add(serverSettingsItem);

            // 2. 通知
            var notifItem = new ToolStripMenuItem(isJa ? "通知" : "Notifications");
            notifItem.Checked = _settings.NotificationsEnabled;
            notifItem.Click += (s, e) => 
            { 
                _settings.NotificationsEnabled = !_settings.NotificationsEnabled;
                _settings.Save();
                UpdateContextMenu();
            };
            settingsItem.DropDownItems.Add(notifItem);

            // 3. Language
            var langItem = new ToolStripMenuItem("言語 (Language)");
            
            var jaItem = new ToolStripMenuItem("日本語");
            jaItem.Click += (s, e) => 
            {
                if (_settings.Language != "ja")
                {
                    _settings.Language = "ja";
                    _settings.Save();
                    UpdateContextMenu();
                }
            };
            if (_settings.Language == "ja") jaItem.Checked = true;
            langItem.DropDownItems.Add(jaItem);

            var enItem = new ToolStripMenuItem("English");
            enItem.Click += (s, e) => 
            {
                if (_settings.Language != "en")
                {
                    _settings.Language = "en";
                    _settings.Save();
                    UpdateContextMenu();
                }
            };
            if (_settings.Language == "en") enItem.Checked = true;
            langItem.DropDownItems.Add(enItem);

            settingsItem.DropDownItems.Add(langItem);

            // 4. 自動起動
            var autoStartItem = new ToolStripMenuItem(isJa ? "自動起動" : "Auto Start");
            autoStartItem.Checked = _settings.AutoStartEnabled;
            autoStartItem.Click += (s, e) => 
            { 
                _settings.AutoStartEnabled = !_settings.AutoStartEnabled;
                SetAutoStart(_settings.AutoStartEnabled);
                _settings.Save();
                UpdateContextMenu();
            };
            settingsItem.DropDownItems.Add(autoStartItem);

            contextMenu.Items.Add(settingsItem);

            contextMenu.Items.Add(isJa ? "更新" : "Refresh", null, (s, e) => TryPoll());
            
            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(isJa ? "終了" : "Exit", null, (s, e) => ExitThread());
            
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ShowSettings()
        {
            using var form = new SettingsForm(_settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _settings = form.GetSettings();
                _settings.Save();
                
                // Immediately apply new interval to animation timer
                int newInterval = _settings.RotationInterval;
                if (newInterval < 1000) newInterval = 1000;
                _animationTimer.Interval = newInterval;
                
                UpdateContextMenu(); 
                TryPoll();
            }
        }

        private void ShowPlayerList()
        {
            var selected = _settings.GetSelectedServer();
            var filtered = _playerHistory.Where(x => x.ServerAddress == selected.Address).ToList();

            bool isPlayersHidden = false;
            if (_lastStatus != null && _lastStatus.Players != null)
            {
                if (_lastStatus.Players.Online > 0 && (_lastStatus.Players.Sample == null || _lastStatus.Players.Sample.Count == 0))
                {
                    isPlayersHidden = true;
                }
            }

            if (_playerListForm == null || _playerListForm.IsDisposed)
            {
                _playerListForm = new PlayerListForm(filtered, _skinManager);
            }
            
            if (isPlayersHidden)
            {
                _playerListForm.ShowMessage("サーバーにより非公開");
            }
            else
            {
                _playerListForm.SetDataSource(filtered);
            }

            if (_playerListForm.Visible)
            {
                _playerListForm.Hide();
            }
            else
            {
                var screen = Screen.FromPoint(Cursor.Position);
                var workingArea = screen.WorkingArea;
                int x = workingArea.Right - _playerListForm.Width - 10;
                int y = workingArea.Bottom - _playerListForm.Height - 10;
                
                _playerListForm.Location = new Point(x, y);
                _playerListForm.Show();
                _playerListForm.Activate();
            }
        }
        
        private void Notify(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            if (!_settings.NotificationsEnabled) return;
            _notifyIcon.ShowBalloonTip(3000, title, message, icon);
        }

        private void InitializeTimers()
        {
            // Polling Timer: Slow, for server data fetch
            _pollTimer = new Timer { Interval = _pollInterval };
            _pollTimer.Tick += _pollTimer_Tick;
            _pollTimer.Start();

            // Animation Timer: Fast, for icon rotation
            int animInterval = _settings.RotationInterval;
            if (animInterval < 1000) animInterval = 1000;
            
            _animationTimer = new Timer { Interval = animInterval };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start(); // Always running from the start
        }
        
        // Wrapper for safe polling call from UI events
        private void TryPoll(bool isManual = false) => Poll(isManual);

        private async void Poll(bool isManual)
        {
             try
            {
                var selected = _settings.GetSelectedServer();
                _lastStatus = await _pinger.PingAsync(selected.Address);
                await UpdateDataAsync(isManual);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Polling Error: {ex.Message}");
                SetDefaultIcon(offline: true);
                if (isManual) Notify("Error", "Update Failed", ToolTipIcon.Error);
            }
        }

        // _pollTimer_Tick delegates to Poll(false)
        public void _pollTimer_Tick(object? sender, EventArgs? e) => Poll(false);
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentSkins.Count == 0)
            {
                // No skins available
                if (_lastStatus == null)
                {
                    // Offline -> Redstone
                    SetDefaultIcon(offline: true);
                }
                else
                {
                    // Online (0 players) -> Chest/Emerald (icon_online)
                    if (_iconOnline != null) 
                    {
                        SetCustomIcon((Icon)_iconOnline.Clone());
                    }
                    else
                    {
                        // Fallback to Main if icon_online is missing
                        SetDefaultIcon(offline: false);
                    }
                }
                return;
            }
            
            _rotationIndex = (_rotationIndex + 1) % _currentSkins.Count;
            ShowSkinIcon(_rotationIndex);
        }
        
        /// <summary>
        /// Display a skin image as the tray icon.
        /// </summary>
        private void ShowSkinIcon(int index)
        {
            if (index < 0 || index >= _currentSkins.Count) return;

            try
            {
                using var bitmap = new Bitmap(_currentSkins[index], new Size(32, 32));
                IntPtr hIcon = bitmap.GetHicon();
                using (var tempIcon = Icon.FromHandle(hIcon))
                {
                    SetCustomIcon((Icon)tempIcon.Clone());
                }
                DestroyIcon(hIcon);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowSkinIcon エラー: {ex.Message}");
            }
        }
        
        private void SetCustomIcon(Icon icon)
        {
            // GDI+ハンドルリークを防ぐため、前のカスタムアイコンを破棄
            DisposeCurrentCustomIcon();
            
            _notifyIcon.Icon = icon;
            _currentCustomIcon = icon;
        }
        
        private void DisposeCurrentCustomIcon()
        {
            if (_currentCustomIcon != null)
            {
                // 現在のアイコンがNotifyIconに割り当てられている場合、使用中は破棄しない方が良いか？
                // 実際にはNotifyIconは参照を保持する。しかし置き換えれば古いものは解放される。
                // ただしIcon.Clone()はコピーを作成する。
                // ベストプラクティス: 新しいものを割り当て、その後古いマネージドラッパーを破棄。
                
                // 注: System.Drawing.Iconはラッパーです。
                _currentCustomIcon.Dispose();
                _currentCustomIcon = null;
            }
        }
        



        /// <summary>
        /// プレイヤーデータと_currentSkinsリストを更新します。
        /// アニメーションタイマーは自動的に変更を取得します。
        /// </summary>
        private async Task UpdateDataAsync(bool isManual)
        {
            var selected = _settings.GetSelectedServer();

            // サーバー切替または初回実行を検出
            if (_lastServerAddress != selected.Address)
            {
                _isFirstPoll = true;
                _lastServerAddress = selected.Address;
                _lastOnlineIds.Clear(); // クリーンスレートを確保
            }

            if (_lastStatus == null)
            {
                _notifyIcon.Text = $"{selected.Alias}: Offline";
                
                // スキンをクリア、破棄を確保
                foreach(var s in _currentSkins) s.Dispose();
                _currentSkins.Clear();
                _rotationIndex = 0;
                
                // 現在のサーバーのプレイヤーをオフラインとしてマーク
                foreach(var p in _playerHistory.Where(x => x.ServerAddress == selected.Address)) 
                {
                    p.IsOnline = false;
                }
                _lastOnlineIds.Clear();
                
                if (isManual) Notify("MineTray", "サーバーがオフラインです。", ToolTipIcon.Warning);
                return;
            }

            var currentSample = _lastStatus.Players?.Sample ?? new List<PlayerSample>();
            var currentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newSkins = new List<Image>();
            bool historyChanged = false;

            // --- 1. 履歴の更新と有効なAPIリクエストのフィルタリング ---
            var playersToFetch = new List<PlayerSample>();

            foreach (var p in currentSample)
            {
                if (string.IsNullOrEmpty(p.Id)) continue;
                if (!Guid.TryParse(p.Id, out _)) continue;

                string cleanName = p.CleanName;
                if (string.IsNullOrWhiteSpace(cleanName) || cleanName.Length > 25) continue;

                currentIds.Add(p.Id);
                playersToFetch.Add(p);
                
                var existing = _playerHistory.FirstOrDefault(h => h.Id == p.Id && h.ServerAddress == selected.Address);
                if (existing == null)
                {
                    existing = new PlayerHistoryItem 
                    { 
                        Id = p.Id, 
                        Name = cleanName,
                        ServerAddress = selected.Address
                    };
                    _playerHistory.Add(existing);
                    historyChanged = true;
                }
                else if (existing.Name != cleanName)
                {
                     existing.Name = cleanName;
                     historyChanged = true;
                }
                
                existing.IsOnline = true;
                existing.LastSeen = DateTime.Now;
            }

            // --- 2. スキンの並列ダウンロード ---
            // Task.WhenAllを使用して並列でスキンを取得
            var skinTasks = playersToFetch.Select(async p => 
            {
                try 
                {
                    return await _skinManager.GetSkinImageAsync(p.Id ?? string.Empty);
                }
                catch 
                {
                    return null; 
                }
            });

            var fetchedImages = await Task.WhenAll(skinTasks);
            foreach (var img in fetchedImages)
            {
                if (img != null) newSkins.Add(img);
            }

            // --- 3. オフラインプレイヤーの処理 ---
            foreach (var p in _playerHistory.Where(x => x.ServerAddress == selected.Address))
            {
                if (!currentIds.Contains(p.Id))
                {
                    if (p.IsOnline) 
                    {
                        p.IsOnline = false;
                    }
                }
            }
            
            if (historyChanged)
            {
                PlayerHistoryManager.Save(_playerHistory);
            }

            // --- 4. スマート参加/退出通知 ---
            CheckAndNotify(currentIds, selected);

            // 手動更新時のフィードバック
            if (isManual)
            {
                int fetchCount = currentIds.Count;
                if (_lastStatus.Players != null && _lastStatus.Players.Online > fetchCount) fetchCount = _lastStatus.Players.Online;
                Notify("MineTray", $"更新完了: {fetchCount}人がオンライン", ToolTipIcon.Info);
            }

            _isFirstPoll = false;
            // 注: _lastOnlineIdsは上記で累積的に更新されています（置換ではなく追加）

            int onlineCount = _lastStatus.Players?.Online ?? 0;
            int maxCount = _lastStatus.Players?.Max ?? 0;
            
            string displayTitle = string.IsNullOrWhiteSpace(selected.Alias) ? selected.Address : selected.Alias;
            string tooltipText = $"{displayTitle}: {onlineCount}/{maxCount}";
            if (tooltipText.Length >= 64) tooltipText = tooltipText.Substring(0, 60) + "...";
            _notifyIcon.Text = tooltipText;

            // --- 5. スキンリストを安全に交換 ---
            // メモリリークを防ぐため古いスキンを破棄
            foreach(var s in _currentSkins) s.Dispose();
            
            _currentSkins = newSkins;
            
            // リストサイズが変わった場合はインデックスをリセット
            if (_currentSkins.Count > 0 && _rotationIndex >= _currentSkins.Count)
            {
                _rotationIndex = 0;
            }

            // オンラインサーバー（0人）用のアイコンを設定
            if (_currentSkins.Count == 0 && _iconOnline != null)
            {
                SetCustomIcon((Icon)_iconOnline.Clone());
            }
        }
        
        private void SetDefaultIcon(bool offline)
        {
            if (offline)
            {
                if (_iconOffline != null) SetCustomIcon((Icon)_iconOffline.Clone());
                else SetFallbackIcon(Color.Gray);
            }
            else
            {
                if (_iconMain != null) SetCustomIcon((Icon)_iconMain.Clone());
                else SetFallbackIcon(Color.LightGreen);
            }
        }

        private void CheckAndNotify(HashSet<string> currentIds, ServerConfig selected)
        {
            _pollCount++;
            
            int onlinePlayers = _lastStatus?.Players?.Online ?? 0;
            int samplePlayers = currentIds.Count;
            // 小規模サーバーの定義: サンプルサイズがオンライン数をカバーするのに十分（かつ少なくとも1人がオンライン）
            bool isSmallServer = (samplePlayers >= onlinePlayers && onlinePlayers > 0);
            
            if (!_isFirstPoll)
            {
                var joinedNames = new List<string>();
                var leftNames = new List<string>();

                // 参加検出: 現在のサンプルにあり、以前見たことがないUUID
                foreach (var id in currentIds)
                {
                    if (!_uuidLastSeenPoll.ContainsKey(id))
                    {
                        var p = _playerHistory.FirstOrDefault(x => x.Id == id && x.ServerAddress == selected.Address);
                        if (p != null) joinedNames.Add(p.Name);
                    }
                }

                // 退出検出
                if (isSmallServer)
                {
                    // 小規模サーバー: 直接検出（完全なリストがあるため）
                    foreach (var id in _lastOnlineIds)
                    {
                        if (!currentIds.Contains(id))
                        {
                            var p = _playerHistory.FirstOrDefault(x => x.Id == id && x.ServerAddress == selected.Address);
                            if (p != null) leftNames.Add(p.Name);
                        }
                    }
                }
                else
                {
                    // 大規模サーバー: 時間ベースの検出
                    var expiredIds = _uuidLastSeenPoll
                        .Where(kv => _pollCount - kv.Value > LEAVE_THRESHOLD_POLLS)
                        .Select(kv => kv.Key)
                        .ToList();
                    
                    foreach (var id in expiredIds)
                    {
                        var p = _playerHistory.FirstOrDefault(x => x.Id == id && x.ServerAddress == selected.Address);
                        if (p != null) leftNames.Add(p.Name);
                        _uuidLastSeenPoll.Remove(id); // クリーンアップ
                    }
                }

                // 通知メッセージの構築
                if (joinedNames.Count > 0 || leftNames.Count > 0)
                {
                    string msg = "";

                    if (joinedNames.Count == 1) msg += $"{joinedNames[0]} が参加";
                    else if (joinedNames.Count > 1) msg += $"{joinedNames.Count}人が参加";

                    if (joinedNames.Count > 0 && leftNames.Count > 0) msg += "、";

                    if (leftNames.Count == 1) msg += $"{leftNames[0]} が退出";
                    else if (leftNames.Count > 1) msg += $"{leftNames.Count}人が退出";
                    
                    if (!string.IsNullOrEmpty(msg))
                    {
                        Notify("MineTray", msg + "しました。", ToolTipIcon.Info);
                    }
                }
            }
            
            // 追跡データを更新
            foreach (var id in currentIds)
            {
                _uuidLastSeenPoll[id] = _pollCount;
                _lastOnlineIds.Add(id);
            }
        }

        private void SetFallbackIcon(Color color)
        {
            try 
            {
                using var bmp = new Bitmap(16, 16);
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.Transparent);
                g.FillRectangle(new SolidBrush(color), 0, 0, 16, 16);
                
                IntPtr hIcon = bmp.GetHicon();
                using (var tempIcon = Icon.FromHandle(hIcon))
                {
                     SetCustomIcon((Icon)tempIcon.Clone());
                }
                DestroyIcon(hIcon);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SetFallbackIcon] Error: {ex.Message}"); }
        } 


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pollTimer?.Stop();
                _animationTimer?.Stop();
                
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                DisposeCurrentCustomIcon();
                
                _pollTimer?.Dispose();
                _animationTimer?.Dispose();
                
                // 全てのスキン画像を破棄
                foreach(var s in _currentSkins) s.Dispose();
                _currentSkins.Clear();

                if (_playerListForm != null && !_playerListForm.IsDisposed)
                    _playerListForm.Dispose();
                    
                 PlayerHistoryManager.Save(_playerHistory);
            }
            base.Dispose(disposing);
        }
    }
}
