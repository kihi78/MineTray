namespace MineTray.Forms
{
    /// <summary>
    /// SettingsForm用のレイアウト定数。
    /// 全ての値はピクセル単位です。
    /// </summary>
    public static class LayoutConstants
    {
        // === ウィンドウ ===
        public const int WindowWidth = 920;
        public const int WindowHeight = 620;
        public const int WindowMinWidth = 800;
        public const int WindowMinHeight = 500;
        public const int SplitterDistance = 420;

        // === サーバー詳細グループ ===
        public const int GroupServerHeight = 270;
        
        // アドレスラベル＆テキストボックス
        public const int LabelAddressX = 20;
        public const int LabelAddressY = 45;
        public const int TextAddressX = 30;
        public const int TextAddressY = 72;
        public const int TextBoxRightMargin = 20;
        
        // エイリアスラベル＆テキストボックス
        public const int LabelAliasX = 20;
        public const int LabelAliasY = 115;
        public const int TextAliasX = 30;
        public const int TextAliasY = 142;
        
        // 監視対象ボタン
        public const int ButtonSetActiveX = 100;
        public const int ButtonSetActiveY = 200;
        public const int ButtonSetActiveWidth = 240;
        public const int ButtonSetActiveHeight = 45;

        // === オプショングループ ===
        public const int GroupOptionsHeight = 130;
        public const int LabelIntervalX = 20;
        public const int LabelIntervalY = 45;
        public const int RadioButtonStartX = 30;
        public const int RadioButtonY = 73;
        public const int RadioButtonGap = 70;
        public const int NumericIntervalX = 340;
        public const int NumericIntervalY = 73;
        public const int NumericIntervalWidth = 70;

        // === 左パネル（サーバーリスト） ===
        public const int LabelListX = 15;
        public const int LabelListY = 7;
        public const int ListServersX = 15;
        public const int ListServersY = 33;
        public const int ListServersHeight = 360; // 0 = 自動
        public const int PanelListButtonsHeight = 60;

        // === ボタン ===
        // 追加/削除ボタン
        public const int ButtonAddX = 16;
        public const int ButtonAddY = 20;
        public const int ButtonAddWidth = 100;
        public const int ButtonAddHeight = 35;
        public const int ButtonDeleteX = 131;
        public const int ButtonDeleteY = 20;
        public const int ButtonDeleteWidth = 100;
        public const int ButtonDeleteHeight = 35;

        // 下部パネルボタン（負のX = 右揃え）
        public const int ButtonResetX = 40;
        public const int ButtonResetY = 0;
        public const int ButtonResetWidth = 120;
        public const int ButtonResetHeight = 40;
        public const int ButtonSaveX = -40; // 右揃え
        public const int ButtonSaveY = 0;
        public const int ButtonSaveWidth = 140;
        public const int ButtonSaveHeight = 45;
    }
}
