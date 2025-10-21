# 実装Todoリスト

## 1. 未実装機能

### 1.1 上り/下り判定機能
**優先度**: 中
**現状**: MelodyPathService.GetAudioPath()で常にisUp = falseを設定
**影響範囲**: ドア閉め案内の音声選択に影響（現在は仮実装で番線の偶数/奇数で判定）
**判定ロジック**: 列車番号（diaName）から判定。偶数列番なら上り、奇数列番なら下り。末尾にSuffixがある場合があるため、最後の数字を抽出して判定。

#### 設計タスク
- [ ] 列車番号から上り/下り判定ロジックの詳細仕様策定
  - 正規表現で列車番号の最後の数字を抽出
  - 偶数判定（偶数=上り、奇数=下り）

#### 実装タスク
- [ ] TraincrewRepositoryまたはMelodyPathServiceに判定メソッド追加
  - メソッド名: `DetermineDirection(string diaName)` → bool (true=上り, false=下り)
  - 正規表現でdiaNameから最後の数字を抽出
  - 数字の偶奇判定
- [ ] MelodyPathServiceの修正
  - GetAudioPath()でTrainState.diaNameから上り/下りを判定
  - GetDoorClosingPath()のisUpパラメータを判定結果で呼び出し
  - ハードコードのisUp=falseを削除

### 1.2 キーボード操作機能（グローバルホットキー）
**優先度**: 高（仕様書の「追加したい機能」に記載）
**現状**: KeyboardRepositoryがコメントアウト状態
**動作**: グローバルホットキーとして実装。自動モード時は無効化。

#### 設計タスク
- [ ] キーボード操作モードの仕様詳細化
  - **モード1**: キーボードを使わない（デフォルト）
  - **モード2**: 2キーでON/OFF（ON用のキーとOFF用のキーを別々に設定）
  - **モード3**: 1キーでON、離すとOFF（キー押下中のみON）
  - **モード4**: 1キーでON、もう一度押すとOFF（トグル動作）
- [ ] 設定ファイル形式の決定
  - JSON推奨: 各モードのキーバインドを柔軟に設定
  - 設定項目: `KeyboardMode` (1-4), `OnKey`, `OffKey`（モード2用）, `ToggleKey`（モード3,4用）
- [ ] グローバルホットキーライブラリの選定
  - Windows API (RegisterHotKey) の使用
  - または、サードパーティライブラリ（例: GlobalHotkeys）

#### 実装タスク
- [ ] キー設定ファイルの設計・作成
  - appsettings.jsonまたは独立したkeyboard.json
  - 例:
    ```json
    {
      "KeyboardMode": 2,
      "OnKey": "F1",
      "OffKey": "F2",
      "ToggleKey": "F3"
    }
    ```
- [ ] KeyboardRepositoryの実装
  - インターフェース定義: `IKeyboardRepository`
  - グローバルホットキー登録機能（RegisterHotKey API使用）
  - 各モードに応じたButtonState返却ロジック
    - モード1: 常にButtonState.NotChanged
    - モード2: OnKey押下→ButtonState.On, OffKey押下→ButtonState.Off
    - モード3: ToggleKey押下→ButtonState.On, 離す→ButtonState.Off
    - モード4: ToggleKey押下→状態トグル（On⇔Off）
  - 設定ファイル読み込み機能
  - ホットキー解除機能（Dispose実装）
- [ ] MainServiceの修正
  - GetButtonState()でKeyboardRepositoryからの入力を考慮
  - 自動モード時はキーボード入力を無視（手動モード時のみ有効）
  - 入力優先順位: UIボタン = キーボード（どちらかが変化したら反映）
- [ ] Program.csの修正
  - KeyboardRepositoryのDI登録（Singleton）
  - Dispose時のホットキー解除

### 1.3 シリアルポート連携機能（物理スイッチ連携）
**優先度**: 低
**現状**: SerialPortRepositoryがコメントアウト状態

#### 設計タスク
- [ ] 物理スイッチの仕様策定
  - 対象デバイス（Arduino等）の決定
  - 通信プロトコルの設計
  - ポート設定方法（設定ファイル）

#### 実装タスク
- [ ] SerialPortRepositoryの実装
  - インターフェース定義: `ISerialPortRepository`
  - シリアルポート接続・切断処理
  - データ受信処理
  - ButtonState返却機能
- [ ] MainServiceの修正
  - GetButtonState()でSerialPortRepositoryからの入力も考慮
- [ ] Program.csの修正
  - SerialPortRepositoryのDI登録

## 2. 設定ファイル化（仕様書の「追加したい機能」）

### 2.1 音声ファイルパス設定ファイル化
**優先度**: 高
**現状**: MelodyPathServiceでハードコード
**目的**: 同じメロディーを複数駅で共有、ユーザー環境に応じたパス指定

#### 設計タスク
- [ ] 設定ファイル形式の決定
  - CSV方式: `駅名,番線,メロディーパス,ドア閉め案内パス`
  - JSON方式: より柔軟な設定が可能
  - 既存Track.csvに統合 vs 別ファイル
- [ ] デフォルト音声の扱い
  - デフォルトパスの設定方法
  - ファイル不存在時のフォールバック処理
- [ ] 相対パス vs 絶対パスの扱い
  - 実行フォルダからの相対パス対応

#### 実装タスク（CSV統合方式の場合）
- [ ] Track.csvの拡張
  - カラム追加: `メロディーファイル名`, `ドア閉め案内ファイル名`
  - 音声ベースディレクトリの設定項目追加（別設定ファイル？）
- [ ] TrackRepositoryの拡張
  - 戻り値に音声ファイル情報を追加
  - または、新規AudioPathRepositoryの作成を検討
- [ ] MelodyPathServiceの修正
  - ハードコードパスを削除
  - TrackRepositoryまたはAudioPathRepositoryから情報取得
  - ファイル存在チェック＆フォールバック処理

#### 実装タスク（別ファイル方式の場合）
- [ ] AudioPath.csvの作成
  - フォーマット: `駅名,番線,メロディーパス,ドア閉め案内パス`
- [ ] AudioPathRepositoryの実装
  - CSVファイル読み込み
  - 駅名・番線から音声パスを取得するメソッド
- [ ] MelodyPathServiceの修正
  - AudioPathRepositoryを使用するように変更

### 2.2 FFmpegパス設定ファイル化
**優先度**: 中
**現状**: FFmpegRepositoryでハードコード

#### 設計タスク
- [ ] 設定ファイル形式の決定
  - アプリケーション設定ファイル（appsettings.json）の導入
  - 簡易的な設定ファイル（config.txt）
- [ ] 環境変数対応の検討

#### 実装タスク
- [ ] 設定ファイルの作成
  - appsettings.json or config.json
  - 設定項目: `FfmpegPath`, `AudioBaseDirectory` 等
- [ ] 設定読み込みクラスの実装
  - IConfiguration使用（appsettings.json方式）
  - または、独自設定クラス
- [ ] FFmpegRepositoryの修正
  - ハードコードパスを削除
  - 設定から読み込んだパスを使用
- [ ] Program.csの修正
  - 設定ファイル読み込みの追加

## 3. UI改善

### 3.1 現在駅・番線の表示（開発者向けモード）
**優先度**: 低
**目的**: デバッグ・動作確認の容易化（通常はユーザーに表示しない）

#### 設計タスク
- [ ] 開発者向けモードの有効化方法
  - プログラムの定数で切り変え(Gitに上げるやつはOffにしといて、開発者側でOnにしてもらうと) 
- [ ] 表示内容の決定
  - 現在駅名・番線
  - 自動モードON/OFF状態
  - メロディー再生状態
  - （オプション）次の駅、発車時刻、列車番号

#### 実装タスク
- [ ] 設定項目の追加
  - AppSettings.csに`DeveloperMode: bool`を追加
  - 設定Windowの「その他」タブに開発者モード切り替えチェックボックス追加
- [ ] MainWindow.xamlの修正
  - 情報表示用のTextBlockまたはLabelを追加（初期状態は非表示）
  - レイアウト調整（既存ボタンに干渉しないように）
- [ ] IMainWindowインターフェースの拡張
  - 駅名・番線表示用メソッド追加
  - 開発者モード表示領域の表示/非表示切り替えメソッド
- [ ] MainWindow.xaml.csの修正
  - 表示更新メソッドの実装
  - 開発者モード設定に応じた表示制御
- [ ] MainServiceの修正
  - 開発者モード有効時のみTickOnPlaying()で駅名・番線情報をUIに反映

### 3.2 自動モードON/OFF切り替え（3.3の設定Windowに統合）
**優先度**: 高（設定Window実装時に含める）
**目的**: 運転士モード時に手動で車掌業務を行いたい場合に対応

#### 実装方針
- 3.3の設定Windowで実装
- 自動モード条件設定（常時ON / 運転士のみON / 常時OFF）
- 強制ON/OFFトグルで一時的に条件を無視可能

### 3.3 設定Window（SettingsWindow）の追加
**優先度**: 高
**目的**: キーボード設定、自動モード設定などをGUIで変更可能にする
**機能**:
- キーボード操作モード設定（モード1-4選択、キーバインド設定）
- 自動モード強制ON/OFF切り替え
- 自動モード条件設定（常時ON / 運転士のみON / 常時OFF）
- その他設定（音量、FFmpegパス等）

#### 設計タスク
- [ ] 設定項目の整理
  - **キーボード設定**:
    - 操作モード選択（ラジオボタン: モード1-4）
    - ONキー設定（TextBox + キー入力検知ボタン）
    - OFFキー設定（TextBox + キー入力検知ボタン）
    - トグルキー設定（TextBox + キー入力検知ボタン）
  - **自動モード設定**:
    - 自動モード条件（ラジオボタン: 常時ON / 運転士のみON / 常時OFF）
    - 強制ON/OFFトグル（CheckBox: 条件を無視して強制的にON/OFF）
  - **その他設定**:
    - FFmpegパス（TextBox + 参照ボタン）
    - 音声ベースディレクトリ（TextBox + 参照ボタン）
    - 開発者モード（CheckBox: デバッグ情報表示の有効化）
    - （オプション）音量調整（スライダー: メロディー音量、ドア閉め音量）※優先度低
- [ ] 設定値の永続化方法
  - appsettings.jsonに保存
  - 設定変更時にファイルへ書き込み
- [ ] ウィンドウ設計
  - モーダルウィンドウ vs モードレスウィンドウ
  - タブ構成（キーボード / 自動モード / その他）
  - OK/キャンセル/適用ボタン

#### 実装タスク
- [ ] 設定データモデルの作成
  - `AppSettings.cs`: 設定値を保持するクラス
    - KeyboardMode, OnKey, OffKey, ToggleKey
    - AutoModeCondition (enum: AlwaysOn, DriverOnly, AlwaysOff)
    - AutoModeForceOverride (bool?)
    - FfmpegPath, AudioBaseDirectory
    - DeveloperMode (bool)
    - （オプション）MelodyVolume, DoorCloseVolume ※優先度低
- [ ] 設定管理Serviceの作成
  - `SettingsService`: appsettings.jsonの読み書きを管理
  - 設定変更通知機能（INotifyPropertyChanged実装）
- [ ] SettingsWindow.xamlの作成
  - タブコントロール構成
    - タブ1: キーボード設定
      - モード選択（RadioButton x4）
      - キーバインド入力欄（TextBox + Button）
    - タブ2: 自動モード設定
      - 条件選択（RadioButton x3）
      - 強制ON/OFFトグル（CheckBox）
    - タブ3: その他設定
      - パス設定 x2（FFmpeg、音声ベースディレクトリ）
      - 開発者モード（CheckBox）
      - （オプション）音量スライダー x2 ※優先度低
  - OK/キャンセル/適用ボタン
- [ ] SettingsWindow.xaml.csの実装
  - 設定値の読み込み・表示
  - キー入力検知機能（ボタン押下でキー入力待ち状態）
  - 設定値の検証（キーの重複チェック等）
  - OK/キャンセル/適用ボタンのイベントハンドラ
  - SettingsServiceへの保存処理
- [ ] MainWindow.xamlの修正
  - 設定ボタン（またはメニュー項目）の追加
  - 設定ボタン押下でSettingsWindowを開く
- [ ] MainServiceの修正
  - SettingsServiceから設定値を取得
  - AutoModeConditionに基づいて自動モード判定ロジックを変更
    - 現在: 運転士=自動、車掌=手動（ハードコード）
    - 変更後: SettingsServiceの設定に従う
  - AutoModeForceOverrideがtrueの場合は強制的に自動モードON/OFF
- [ ] KeyboardRepositoryの修正
  - SettingsServiceから設定値を取得
  - 設定変更時にホットキーを再登録
- [ ] AudioPlayerRepositoryの修正
  - SettingsServiceから音量設定を取得
  - MediaPlayer.Volumeプロパティに反映
- [ ] Program.csの修正
  - SettingsServiceのDI登録
  - appsettings.jsonの読み込み設定

### 3.4 音量調整機能（3.3の設定Windowに統合）
**優先度**: 低（オプション機能）
**目的**: ゲーム音とのバランス調整

#### 実装タスク
- [ ] 3.3の設定Window実装時にオプションとして含める
  - メロディー音量スライダー
  - ドア閉め案内音量スライダー
  - AudioPlayerRepositoryへの音量設定反映
  - ※必須機能ではないため、後回し可

## 4. 既知の問題修正

### 4.1 MediaPlayerによる音声長さ取得の問題対応
**優先度**: 完了（FFmpegで対応済み）
**現状**: 要件定義書65行目に記載の通り、FFmpegで実装済み

## 5. テスト・ドキュメント

### 5.1 単体テストの追加
**優先度**: 中

#### タスク
- [ ] テストプロジェクトの作成
- [ ] Repositoryレイヤーのテスト
  - TrackRepositoryのCSV読み込みテスト
  - FFmpegRepositoryのパーステスト（モック使用）
- [ ] Serviceレイヤーのテスト
  - AutoModeServiceのタイミング計算テスト
  - MelodyPathServiceのパス決定テスト

### 5.2 ユーザーマニュアルの作成
**優先度**: 低

#### タスク
- [ ] インストール手順書
- [ ] 設定ファイルの編集方法
- [ ] トラブルシューティングガイド

## 6. リファクタリング（任意）

### 6.1 設定管理の統一
**優先度**: 低

#### タスク
- [ ] IConfigurationの全面導入
- [ ] appsettings.jsonへの統一
- [ ] 環境別設定対応（開発/本番）

### 6.2 エラーハンドリングの強化
**優先度**: 低

#### タスク
- [ ] カスタム例外クラスの定義
- [ ] ログ出力の充実化
- [ ] ユーザーへのエラー通知UI

## 実装優先順位まとめ

### 高優先度（仕様書の「追加したい機能」＋ユーザビリティ）
1. **設定Window（SettingsWindow）の追加** (3.3) ★最重要
   - キーボード操作設定
   - 自動モード条件設定・強制切り替え
   - FFmpegパス等の設定
   - 開発者モード設定
2. **音声ファイルパス設定ファイル化** (2.1)
3. **キーボード操作機能** (1.2)

### 中優先度（利便性向上）
4. **上り/下り判定機能** (1.1)
5. **単体テストの追加** (5.1)

### 低優先度（拡張機能・開発者向け）
6. **現在駅・番線の表示（開発者向けモード）** (3.1)
7. **音量調整機能** (3.4)
8. **シリアルポート連携機能** (1.3)
9. **ユーザーマニュアルの作成** (5.2)
10. **リファクタリング** (6.1, 6.2)

## 推奨実装順序

### Phase 1: 設定基盤の整備
1. appsettings.jsonの導入とIConfigurationのセットアップ
2. SettingsServiceの実装（設定の読み書き管理）
3. AppSettings.csの作成（設定データモデル）

### Phase 2: 設定Window実装（最優先）
4. SettingsWindow.xaml/.xaml.csの実装
   - タブ1: キーボード設定
   - タブ2: 自動モード設定
   - タブ3: その他設定（音量、パス等）
5. MainWindowへの設定ボタン追加

### Phase 3: 各機能の設定Window連携
6. キーボード操作機能の実装（SettingsServiceと連携）
7. 音声ファイルパス設定ファイル化（SettingsServiceと連携）
8. 自動モード判定ロジックの変更（SettingsServiceと連携）

### Phase 4: 細かい改善
9. 上り/下り判定機能
10. 単体テストの追加

### Phase 5: 低優先度機能（必要に応じて実装）
11. 現在駅・番線の表示（開発者向けモード）
12. 音量調整機能
13. その他の低優先度機能

## 実装時の注意点

### 設定Windowを最優先で実装する理由
- キーボード操作、自動モード設定、音量調整など複数機能の設定を一元管理
- ユーザーがGUIで設定変更できることで使いやすさが大幅に向上
- 後続の機能実装（キーボード、音声パス等）が設定Windowに依存
- appsettings.json基盤を最初に整備することで、後の実装がスムーズ

### 実装の依存関係
```
Phase 1（設定基盤）
    ↓
Phase 2（設定Window）
    ↓
Phase 3（各機能） ← 並行実装可能
    ↓
Phase 4-5（改善・拡張）
```
