using FluentAssertions;
using Traincrew_depMelody.Enum;
using Traincrew_depMelody.Window;

namespace Traincrew_depMelody.Tests.Presentation;

public class MainWindowTests
{
    // Note: MainWindowのテストは以下の理由で非常に困難:
    // 1. WPF UIスレッド(STA thread)が必要
    // 2. Dispatcher、ServiceScopeFactory、実際のボタンコントロールへの依存
    // 3. XAMLファイルとの結合(InitializeComponent)
    //
    // これらのテストは以下のいずれかのアプローチで実装することを推奨:
    // - WPF Test Framework (FlaUI, White Framework等)
    // - UIテストを手動テストとして実施
    // - MainWindowのロジックをViewModelに分離してMVVMパターンに移行

    [Fact]
    public void MW001_IMainWindowインターフェース確認()
    {
        // IMainWindowインターフェースが正しく定義されていることを確認
        var interfaceType = typeof(IMainWindow);
        interfaceType.Should().NotBeNull();
        interfaceType.IsInterface.Should().BeTrue();

        // 必要なメソッドが定義されていることを確認
        interfaceType.GetMethod("SetButtonIsEnabled").Should().NotBeNull();
        interfaceType.GetMethod("SetTopMost").Should().NotBeNull();
        interfaceType.GetMethod("GetButtonState").Should().NotBeNull();
        interfaceType.GetMethod("GetAudioPlayerRepository").Should().NotBeNull();
    }

    [Fact]
    public void MW002_ButtonState列挙型確認()
    {
        // ButtonState列挙型が正しく定義されていることを確認
        var onState = ButtonState.On;
        var offState = ButtonState.Off;
        var notChangedState = ButtonState.NotChanged;

        onState.Should().NotBe(offState);
        onState.Should().NotBe(notChangedState);
        offState.Should().NotBe(notChangedState);
    }

    // MW-003以降のテストは実際のWPF環境が必要なため、
    // 統合テストまたはE2Eテストとして別途実装することを推奨
}
