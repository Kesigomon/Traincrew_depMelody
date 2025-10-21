using FluentAssertions;
using Traincrew_depMelody.Repository;

namespace Traincrew_depMelody.Tests.Repository;

public class FFmpegRepositoryTests
{
    private readonly FFmpegRepository _repository;

    public FFmpegRepositoryTests()
    {
        _repository = new FFmpegRepository();
    }

    // Note: FF-001からFF-007のテストは実際のFFmpegと音声ファイルが必要なため、
    // 統合テストとして別途実装するか、FFmpegのモックが必要

    // FF-101, FF-102, FF-103は正規表現のテストだが、
    // 正規表現はprivateメソッド内で使用されているため、
    // 直接テストできない。リフレクションを使うか、
    // GetDuration()の統合テストとして実装する必要がある

    [Fact]
    public void FF008_Repository作成()
    {
        // FFmpegRepositoryが正常に作成できることを確認
        var repository = new FFmpegRepository();
        repository.Should().NotBeNull();
    }

    // 実際のFFmpegを使ったテストは環境依存のため、
    // CI/CDパイプラインでFFmpegがインストールされている場合のみ実行可能
    // または、モックを使った単体テストが必要
}
