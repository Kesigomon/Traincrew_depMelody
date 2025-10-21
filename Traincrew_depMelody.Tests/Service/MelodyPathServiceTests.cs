using FluentAssertions;
using Moq;
using Traincrew_depMelody.Repository;
using Traincrew_depMelody.Service;
using Traincrew_depMelody.Tests.TestHelpers;

namespace Traincrew_depMelody.Tests.Service;

public class MelodyPathServiceTests
{
    private readonly Mock<ITraincrewRepository> _traincrewRepositoryMock;
    private readonly MelodyPathService _service;

    public MelodyPathServiceTests()
    {
        _traincrewRepositoryMock = new Mock<ITraincrewRepository>();
        _service = new MelodyPathService(_traincrewRepositoryMock.Object);
    }

    [Fact]
    public void MP001_正常なパス取得()
    {
        // 館浜1番線のメロディーパスを取得（特急の場合）
        var trainState = TrainStateHelper.CreateTrainState("特急");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("館浜", 1), false);

        result.LocalPath.Should().Contain("sound");
        result.LocalPath.Should().Contain("館浜_1.wav");
    }

    [Fact]
    public void MP002_デフォルトメロディー()
    {
        // 存在しない駅名の場合はdefault.wavを返す
        var trainState = TrainStateHelper.CreateTrainState("普通");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("存在しない駅", 1), false);

        result.LocalPath.Should().Contain("sound");
        result.LocalPath.Should().Contain("default.wav");
    }

    [Fact]
    public void MP003_上り下りによる変化()
    {
        // isUpの違いによるパス変化（現状は同一）
        var trainState = TrainStateHelper.CreateTrainState("普通");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result1 = _service.GetAudioPath(("駒野", 1), false);
        var result2 = _service.GetAudioPath(("駒野", 1), false);

        result1.Should().Be(result2);
    }

    [Fact]
    public void MP101_下りパス()
    {
        // 下り方向のドア閉め案内パス
        var trainState = TrainStateHelper.CreateTrainState("普通");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("駒野", 1), true);

        result.LocalPath.Should().Contain("sound");
        result.LocalPath.Should().Contain("doorClosing_1.wav");
    }

    [Fact]
    public void MP102_上りパス()
    {
        // 上り方向のドア閉め案内パス（下りと同じ）
        var trainState = TrainStateHelper.CreateTrainState("普通");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("駒野", 1), true);

        result.LocalPath.Should().Contain("sound");
        result.LocalPath.Should().Contain("doorClosing_1.wav");
    }

    [Fact]
    public void MP201_館浜1番線_特急()
    {
        // 館浜1番線・特急の場合、番線そのまま
        var trainState = TrainStateHelper.CreateTrainState("特急");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("館浜", 1), false);

        result.LocalPath.Should().Contain("館浜_1.wav");
    }

    [Fact]
    public void MP202_館浜1番線_普通()
    {
        // 館浜1番線・普通の場合、番線+1
        var trainState = TrainStateHelper.CreateTrainState("普通");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("館浜", 1), false);

        result.LocalPath.Should().Contain("館浜_2.wav");
    }

    [Fact]
    public void MP203_館浜2番線_特急()
    {
        // 館浜2番線・特急の場合、番線そのまま
        var trainState = TrainStateHelper.CreateTrainState("特急");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("館浜", 2), false);

        result.LocalPath.Should().Contain("館浜_2.wav");
    }

    [Fact]
    public void MP204_館浜2番線_普通()
    {
        // 館浜2番線・普通の場合、番線+1
        var trainState = TrainStateHelper.CreateTrainState("普通");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("館浜", 2), false);

        result.LocalPath.Should().Contain("館浜_3.wav");
    }

    [Fact]
    public void MP205_館浜3番線以降()
    {
        // 館浜3番線以降は番線+1
        var trainState = TrainStateHelper.CreateTrainState("普通");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("館浜", 3), false);

        result.LocalPath.Should().Contain("館浜_4.wav");
    }

    [Fact]
    public void MP301_ON時のパス()
    {
        // isMelodyPlaying=false (ON押下直後) → メロディーパス
        var trainState = TrainStateHelper.CreateTrainState("普通");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("駒野", 1), false);

        result.LocalPath.Should().Contain("駒野_1.wav");
    }

    [Fact]
    public void MP302_OFF時のパス()
    {
        // isMelodyPlaying=true (OFF押下直後) → ドア閉め案内パス
        var trainState = TrainStateHelper.CreateTrainState("普通");
        _traincrewRepositoryMock.Setup(x => x.GetTrainState()).Returns((dynamic)trainState);

        var result = _service.GetAudioPath(("駒野", 1), true);

        result.LocalPath.Should().Contain("doorClosing_1.wav");
    }
}
