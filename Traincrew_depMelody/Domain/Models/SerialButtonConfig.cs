namespace Traincrew_depMelody.Domain.Models;

/// <summary>
///     シリアルポートボタンの入力ピン種別
/// </summary>
public enum SerialInputPin
{
    Dsr,
    Cts,
    Cd
}

/// <summary>
///     シリアルポートボタン設定
/// </summary>
public class SerialButtonConfig
{
    /// <summary>
    ///     ポート名 (例: "COM3"。空文字なら未接続)
    /// </summary>
    public string PortName { get; set; } = "";

    /// <summary>
    ///     ボーレート
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    ///     DTR (Data Terminal Ready) を有効にするか
    /// </summary>
    public bool DtrEnable { get; set; } = true;

    /// <summary>
    ///     RTS (Request To Send) を有効にするか
    /// </summary>
    public bool RtsEnable { get; set; } = false;

    /// <summary>
    ///     監視する入力ピン
    /// </summary>
    public SerialInputPin InputPin { get; set; } = SerialInputPin.Dsr;

    /// <summary>
    ///     ポーリング間隔 (ミリ秒)
    /// </summary>
    public int PollingIntervalMs { get; set; } = 100;

    /// <summary>
    ///     論理反転フラグ (配線都合でON/OFFが逆になる場合に true に設定)
    /// </summary>
    public bool Inverted { get; set; } = false;
}
