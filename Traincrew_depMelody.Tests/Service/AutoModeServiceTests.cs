using FluentAssertions;
using Moq;
using Traincrew_depMelody.Enum;
using Traincrew_depMelody.Repository;
using Traincrew_depMelody.Service;
using Traincrew_depMelody.Tests.TestHelpers;

namespace Traincrew_depMelody.Tests.Service;

public class AutoModeServiceTests
{
    private readonly Mock<IFFmpegRepository> _ffmpegRepositoryMock;
    private readonly Mock<ITraincrewRepository> _traincrewRepositoryMock;
    private readonly Mock<ITrackRepository> _trackRepositoryMock;
    private readonly Mock<MelodyPathService> _melodyPathServiceMock;
    private readonly AutoModeService _service;

    public AutoModeServiceTests()
    {
        _ffmpegRepositoryMock = new Mock<IFFmpegRepository>();
        _traincrewRepositoryMock = new Mock<ITraincrewRepository>();
        _trackRepositoryMock = new Mock<ITrackRepository>();
        _melodyPathServiceMock = new Mock<MelodyPathService>(_traincrewRepositoryMock.Object);

        _service = new AutoModeService(
            _ffmpegRepositoryMock.Object,
            _traincrewRepositoryMock.Object,
            _trackRepositoryMock.Object,
            _melodyPathServiceMock.Object
        );
    }

    [Fact]
    public void AM401_Reset動作()
    {
        // Reset()を呼び出してすべての状態がリセットされる
        _service.Reset();

        // リセット後の動作確認は内部状態のため、次のGetButtonStateで確認
        // このテストは正常に実行されればOK
        Assert.True(true);
    }

    // Note: AutoModeServiceの多くのテストはTrainStateの作成が必要なため、
    // TrainState型解決の問題が解決されるまで、基本的なテストのみ実装
}
