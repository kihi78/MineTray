using MineTray.Models;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System;

namespace MineTray.Forms
{
    /// <summary>
    /// MineTrayの設定画面フォーム。
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        
#nullable disable
        // UIコントロール - シングルページレイアウト
        // これらのフィールドはInitializeComponent()で初期化されるため、null許容を無効化
        private ListBox _lstServers; 
        private TextBox _txtAddress;
        private TextBox _txtAlias;
        private Button _btnAddServer;
        private Button _btnRemoveServer;
        private Button _btnSetMain;
        
        private NumericUpDown _numInterval;
        private RadioButton _rb3s;
        private RadioButton _rb5s;
        private RadioButton _rb10s;
        private RadioButton _rbCustom;
        
        private Button _btnSave;
        private Button _btnReset;

        // 動的レイアウト用フィールド
        private SplitContainer _split;
        private GroupBox _grpServer;
        private GroupBox _grpOpt;
        private Label _lblList;
        private Label _lblAddr;
        private Label _lblAlias;
        private Label _lblInt;
        private Panel _pnlListBtns;
        private Panel _pnlSpacer;
        private Panel _pnlBottom;
#nullable restore

        // 編集中のサーバーリスト（クローン。ライブ編集を防止）
        private List<ServerConfig> _tempServerList;
        // イベントの再入を防止するフラグ
        private bool _isUpdating = false;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            // 編集分離のためのディープコピー
            _tempServerList = _settings.Servers.Select(s => new ServerConfig 
            { 
                Address = s.Address, 
                Alias = s.Alias, 
                IsSelected = s.IsSelected 
            }).ToList();

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            bool isEn = _settings.Language == "en";
            this.Text = isEn ? "MineTray Settings" : "MineTray 設定";
            
            // ウィンドウの初期設定
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular); // Reduced Font Size
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            if (MineTrayApplicationContext.MainIcon != null)
            {
                this.Icon = MineTrayApplicationContext.MainIcon;
            }

            // --- コントロールの作成 ---

            _split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            
            // --- 左パネル ---


            _pnlListBtns = new Panel { Dock = DockStyle.None, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            _btnAddServer = new Button { Text = isEn ? "Add" : "追加", AutoSize = true };
            _btnAddServer.Click += (s, e) => AddServer();
            _btnRemoveServer = new Button { Text = isEn ? "Delete" : "削除", AutoSize = true };
            _btnRemoveServer.Click += (s, e) => RemoveServer();
            
            _pnlListBtns.Controls.Add(_btnAddServer);
            _pnlListBtns.Controls.Add(_btnRemoveServer);
            
            // --- LEFT PANEL ---
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) }; // コンテナ
            
            // レイアウト用に明示的にDock=Noneを設定
            _lblList = new Label { Text = isEn ? "Server List" : "サーバーリスト", AutoSize = true };
            _lstServers = new ListBox { IntegralHeight = false, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            _lstServers.DisplayMember = "Alias";
            _lstServers.SelectedIndexChanged += LstServers_SelectedIndexChanged;

            pnlLeft.Controls.Add(_lstServers); 
            pnlLeft.Controls.Add(_pnlListBtns);
            pnlLeft.Controls.Add(_lblList);
            _split.Panel1.Controls.Add(pnlLeft);

            // --- 右パネル ---
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            
            // サーバー詳細グループ
            _grpServer = new GroupBox { Text = isEn ? "Selected Server Details" : "選択中のサーバー詳細", Dock = DockStyle.Top, Padding = new Padding(10) };
            
            _lblAddr = new Label { Text = isEn ? "Server Address:" : "サーバーアドレス:", AutoSize = true };
            _txtAddress = new TextBox { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Font = new Font("Segoe UI", 9F) };
            _txtAddress.TextChanged += (s, e) => UpdateCurrentServer();
            // _txtAddress.Leave は削除 (UpdateCurrentServerで自動保存)
            
            _lblAlias = new Label { Text = isEn ? "Display Name:" : "表示名:", AutoSize = true };
            _txtAlias = new TextBox { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Font = new Font("Segoe UI", 9F) };
            _txtAlias.TextChanged += (s, e) => UpdateCurrentServer();
            _txtAlias.Leave += (s, e) => RefreshListDisplay(); // フォーカスアウト時にリスト表示を更新
            
            _btnSetMain = new Button { Text = isEn ? "Set as Active" : "監視対象にする", AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _btnSetMain.Click += (s, e) => SetActiveServer();

            _grpServer.Controls.AddRange(new Control[] { _lblAddr, _txtAddress, _lblAlias, _txtAlias, _btnSetMain });
            
            _pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 20 };
            
            // アプリ設定
            _grpOpt = new GroupBox { Text = isEn ? "Application Options" : "アプリ設定", Dock = DockStyle.Top };
            
            _lblInt = new Label { Text = isEn ? "Rotation:" : "切替間隔:", AutoSize = true };
            
            _rb3s = new RadioButton { Text = "3s", AutoSize = true };
            _rb5s = new RadioButton { Text = "5s", AutoSize = true };
            _rb10s = new RadioButton { Text = "10s", AutoSize = true };
            _rbCustom = new RadioButton { Text = isEn ? "Custom" : "カスタム", AutoSize = true };
            _numInterval = new NumericUpDown { Minimum = 1, Maximum = 60, DecimalPlaces = 0, Enabled = false };
            _rbCustom.CheckedChanged += (s, e) => _numInterval.Enabled = _rbCustom.Checked;

            _grpOpt.Controls.AddRange(new Control[] { _lblInt, _rb3s, _rb5s, _rb10s, _rbCustom, _numInterval });

            pnlRight.Controls.Add(_grpOpt);
            pnlRight.Controls.Add(_pnlSpacer);
            pnlRight.Controls.Add(_grpServer);
            _split.Panel2.Controls.Add(pnlRight);
            
            _pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(20) };
            _btnReset = new Button { Text = isEn ? "Reset" : "初期化", ForeColor = Color.Red, AutoSize = true };
            _btnReset.Click += BtnReset_Click;
            _btnSave = new Button { Text = isEn ? "Save" : "設定を保存", DialogResult = DialogResult.OK, AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            _btnSave.Click += BtnSave_Click;
            
            _pnlBottom.Controls.Add(_btnReset);
            _pnlBottom.Controls.Add(_btnSave);

            this.Controls.Add(_split);
            this.Controls.Add(_pnlBottom);

            // 固定レイアウトを適用
            ApplyFixedLayout();
        }

        private void ApplyFixedLayout()
        {
            // LayoutConstantsクラスの定数を使用
            var L = typeof(LayoutConstants); // 短縮用エイリアス
            
            int winW = LayoutConstants.WindowWidth;
            int winH = LayoutConstants.WindowHeight;
            int splitDist = LayoutConstants.SplitterDistance;
            int grpServerH = LayoutConstants.GroupServerHeight;
            int lbAddrX = LayoutConstants.LabelAddressX;
            int lbAddrY = LayoutConstants.LabelAddressY;
            int txAddrX = LayoutConstants.TextAddressX;
            int txAddrY = LayoutConstants.TextAddressY;
            int lbAliasX = LayoutConstants.LabelAliasX;
            int lbAliasY = LayoutConstants.LabelAliasY;
            int txAliasX = LayoutConstants.TextAliasX;
            int txAliasY = LayoutConstants.TextAliasY;
            int btMainX = LayoutConstants.ButtonSetActiveX;
            int btMainY = LayoutConstants.ButtonSetActiveY;
            int btMainW = LayoutConstants.ButtonSetActiveWidth;
            int btMainH = LayoutConstants.ButtonSetActiveHeight;
            int grpOptH = LayoutConstants.GroupOptionsHeight;
            int lbIntX = LayoutConstants.LabelIntervalX;
            int lbIntY = LayoutConstants.LabelIntervalY;
            int rbStartX = LayoutConstants.RadioButtonStartX;
            int rbY = LayoutConstants.RadioButtonY;
            int rbGap = LayoutConstants.RadioButtonGap;
            int numIntX = LayoutConstants.NumericIntervalX;
            int numIntY = LayoutConstants.NumericIntervalY;
            int numIntW = LayoutConstants.NumericIntervalWidth;
            
            // Buttons
            int btAddX = LayoutConstants.ButtonAddX;
            int btAddY = LayoutConstants.ButtonAddY;
            int btAddW = LayoutConstants.ButtonAddWidth;
            int btAddH = LayoutConstants.ButtonAddHeight;
            int btDelX = LayoutConstants.ButtonDeleteX;
            int btDelY = LayoutConstants.ButtonDeleteY;
            int btDelW = LayoutConstants.ButtonDeleteWidth;
            int btDelH = LayoutConstants.ButtonDeleteHeight;
            int btResetX = LayoutConstants.ButtonResetX;
            int btResetY = LayoutConstants.ButtonResetY;
            int btResetW = LayoutConstants.ButtonResetWidth;
            int btResetH = LayoutConstants.ButtonResetHeight;
            int btSaveX = LayoutConstants.ButtonSaveX;
            int btSaveY = LayoutConstants.ButtonSaveY;
            int btSaveW = LayoutConstants.ButtonSaveWidth;
            int btSaveH = LayoutConstants.ButtonSaveHeight;

            // List
            int lblListX = LayoutConstants.LabelListX;
            int lblListY = LayoutConstants.LabelListY;
            int lstServersX = LayoutConstants.ListServersX;
            int lstServersY = LayoutConstants.ListServersY;
            int lstServersW = 0; // Auto-calculated
            int lstServersH = LayoutConstants.ListServersHeight;
            int pnlListBtnsH = LayoutConstants.PanelListButtonsHeight;



            // Apply to Controls
            this.Size = new Size(winW, winH);
            this.MinimumSize = new Size(LayoutConstants.WindowMinWidth, LayoutConstants.WindowMinHeight);
            
            if (_split != null) _split.SplitterDistance = splitDist;

            // Helper to handle negative coordinates (Right/Bottom alignment)
            Point GetPos(Control ctrl, int x, int y, Control parent)
            {
                int finalX = x >= 0 ? x : parent.ClientSize.Width + x - ctrl.Width;
                int finalY = y >= 0 ? y : parent.ClientSize.Height + y - ctrl.Height;
                return new Point(finalX, finalY);
            }

            if (_pnlListBtns != null && _split != null) 
            {
                int p1W = _split.Panel1.ClientSize.Width;
                int p1H = _split.Panel1.ClientSize.Height;

                _pnlListBtns.Size = new Size(p1W, pnlListBtnsH);
                _pnlListBtns.Location = new Point(0, p1H - pnlListBtnsH);
                
                if (_btnAddServer != null) { _btnAddServer.Location = new Point(btAddX, btAddY); _btnAddServer.Width = btAddW; _btnAddServer.Height = btAddH; }
                if (_btnRemoveServer != null) { _btnRemoveServer.Location = new Point(btDelX, btDelY); _btnRemoveServer.Width = btDelW;  _btnRemoveServer.Height = btDelH; }
            }

            if (_lblList != null) { _lblList.Location = new Point(lblListX, lblListY); }
            
            if (_lstServers != null && _split != null) 
            { 
                int p1W = _split.Panel1.ClientSize.Width;
                int p1H = _split.Panel1.ClientSize.Height;

                int finalX = lstServersX;
                int finalY = lstServersY;
                int finalW = (lstServersW > 0) ? lstServersW : (p1W - lstServersX * 2); 
                int finalH = (lstServersH > 0) ? lstServersH : (p1H - lstServersY - pnlListBtnsH - 10); 

                _lstServers.Location = new Point(finalX, finalY);
                _lstServers.Size = new Size(finalW, finalH);
            }

            if (_grpServer != null) _grpServer.Height = grpServerH;
            
            if (_lblAddr != null) _lblAddr.Location = new Point(lbAddrX, lbAddrY);
            if (_txtAddress != null && _grpServer != null) 
            { 
                _txtAddress.Location = new Point(txAddrX, txAddrY); 
                // Auto width for TextBox
                _txtAddress.Width = _grpServer.ClientSize.Width - txAddrX - 20; 
                _txtAddress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; 
            }

            if (_lblAlias != null) _lblAlias.Location = new Point(lbAliasX, lbAliasY);
            if (_txtAlias != null && _grpServer != null) 
            { 
                _txtAlias.Location = new Point(txAliasX, txAliasY); 
                // Auto width for TextBox
                _txtAlias.Width = _grpServer.ClientSize.Width - txAliasX - 20;
                _txtAlias.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }

            if (_btnSetMain != null && _grpServer != null) 
            { 
                _btnSetMain.AutoSize = false; 
                _btnSetMain.Location = new Point(btMainX, btMainY); 
                
                // Fixed Width Logic for btMain as requested
                _btnSetMain.Width = btMainW;
                _btnSetMain.Anchor = AnchorStyles.Top | AnchorStyles.Left; 

                _btnSetMain.Height = btMainH; 
            }

            if (_grpOpt != null) _grpOpt.Height = grpOptH;

            if (_lblInt != null) _lblInt.Location = new Point(lbIntX, lbIntY);
            if (_rb3s != null) _rb3s.Location = new Point(rbStartX, rbY);
            if (_rb5s != null) _rb5s.Location = new Point(rbStartX + rbGap, rbY);
            if (_rb10s != null) _rb10s.Location = new Point(rbStartX + rbGap*2, rbY);
            if (_rbCustom != null) _rbCustom.Location = new Point(rbStartX + rbGap*3, rbY);
            if (_numInterval != null) { _numInterval.Location = new Point(numIntX, numIntY); _numInterval.Width = numIntW; }

            // Bottom Panel Buttons
            if (_pnlBottom != null)
            {
                if (_btnReset != null) { _btnReset.Width = btResetW; _btnReset.Height = btResetH; _btnReset.Location = GetPos(_btnReset, btResetX, btResetY, _pnlBottom); }
                if (_btnSave != null) { _btnSave.Width = btSaveW; _btnSave.Height = btSaveH;  _btnSave.Location = GetPos(_btnSave, btSaveX, btSaveY, _pnlBottom); }
                // Debug button removed
            }

            this.Refresh();
        }

        private void LoadData()
        {
            // Restore last custom interval value
            _numInterval.Value = _settings.LastCustomInterval > 0 && _settings.LastCustomInterval <= 60 ? _settings.LastCustomInterval : 3;

            // Rotation Interval
            int sec = _settings.RotationInterval / 1000;
            if (sec == 3) _rb3s.Checked = true;
            else if (sec == 5) _rb5s.Checked = true;
            else if (sec == 10) _rb10s.Checked = true;
            else { 
                _rbCustom.Checked = true; 
                _numInterval.Value = sec > 0 ? sec : 3; 
            }

            RefreshServerList();
        }

        private void RefreshServerList()
        {
            _lstServers.BeginUpdate();
            _lstServers.Items.Clear();
            foreach (var s in _tempServerList) _lstServers.Items.Add(s);
            _lstServers.EndUpdate();
            
            var selected = _tempServerList.FirstOrDefault(x => x.IsSelected);
            if (selected != null) _lstServers.SelectedItem = selected;
            else if (_tempServerList.Count > 0) _lstServers.SelectedIndex = 0;
            else LstServers_SelectedIndexChanged(null, EventArgs.Empty); // Clear fields
        }
        
        private void RefreshListDisplay()
        {
             int idx = _lstServers.SelectedIndex;
             if (idx >= 0)
             {
                 _isUpdating = true;
                 _lstServers.Items[idx] = _lstServers.Items[idx];
                 _isUpdating = false;
             }
        }
        
        private void LstServers_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isUpdating) return; // Skip if we're just refreshing display
            
            bool hasSelection = _lstServers.SelectedItem is ServerConfig;
            
            _txtAddress.Enabled = hasSelection;
            _txtAlias.Enabled = hasSelection;
            
            // Prevent TextChanged from triggering saves during item switch
            _isUpdating = true;
            
            if (hasSelection)
            {
                var s = (ServerConfig)_lstServers.SelectedItem;
                _txtAddress.Text = s.Address;
                _txtAlias.Text = s.Alias;
                
                _btnSetMain.Enabled = !s.IsSelected;
                _btnSetMain.Text = s.IsSelected ? (_settings.Language == "en" ? "(Active)" : "(監視中)") : (_settings.Language == "en" ? "Set as Active" : "監視対象にする");
            }
            else
            {
                _txtAddress.Text = "";
                _txtAlias.Text = "";
                _btnSetMain.Enabled = false;
            }
            
            _isUpdating = false;
        }
        
        private void UpdateCurrentServer()
        {
            if (_isUpdating) return; // Skip during item switch to prevent data corruption
            
            if (_lstServers.SelectedItem is ServerConfig s)
            {
                s.Address = _txtAddress.Text;
                s.Alias = _txtAlias.Text;
                
                // Real-time save to ensure persistence
                SaveCurrentSettings();
            }
        }
        
        private void SaveCurrentSettings()
        {
            // Copy current temp list to settings and save to disk
            _settings.Servers.Clear();
            _settings.Servers.AddRange(_tempServerList);
            _settings.Save();
        }
        
        private void AddServer()
        {
            var s = new ServerConfig { Address = "localhost", Alias = "New Server" };
            _tempServerList.Add(s);
            _lstServers.Items.Add(s);
            _lstServers.SelectedItem = s;
            SaveCurrentSettings(); // Auto-save when adding server
        }
        
        private void RemoveServer()
        {
            if (_lstServers.SelectedItem is ServerConfig s)
            {
                _tempServerList.Remove(s);
                RefreshServerList();
                SaveCurrentSettings(); // Auto-save when removing server
            }
        }
        
        private void SetActiveServer()
        {
            if (_lstServers.SelectedItem is ServerConfig s)
            {
                foreach(var x in _tempServerList) x.IsSelected = false;
                s.IsSelected = true;
                LstServers_SelectedIndexChanged(null, null); // Update button state
            }
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Reset ALL data?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _tempServerList.Clear();
                _tempServerList.Add(new ServerConfig { Address = "localhost", Alias = "New Server", IsSelected = true });
                RefreshServerList();
                try { if (File.Exists("players.json")) File.Delete("players.json"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[BtnReset_Click] Error deleting players.json: {ex.Message}"); }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // Save Values
            
            int interval = 3;
            if (_rb3s.Checked) interval = 3;
            else if (_rb5s.Checked) interval = 5;
            else if (_rb10s.Checked) interval = 10;
            else if (_rbCustom.Checked) interval = (int)_numInterval.Value;
            if (interval < 1) interval = 1;
            _settings.RotationInterval = interval * 1000;
            
            // Save Last Custom Interval (Always, so user preferences are dynamic)
            _settings.LastCustomInterval = (int)_numInterval.Value;

            // Apply Server List Changes - Copy from temp list to settings
            _settings.Servers.Clear();
            _settings.Servers.AddRange(_tempServerList);
            
            // Immediately persist to disk
            _settings.Save();
        }

        public AppSettings GetSettings()
        {
            return _settings;
        }
    }
}
