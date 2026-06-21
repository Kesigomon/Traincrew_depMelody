using Traincrew_depMelody.Domain.Interfaces.Services;
using Traincrew_depMelody.Domain.Models;

namespace Traincrew_depMelody.Tests.Infrastructure.ExternalServices;

/// <summary>
/// UT用の決定論的なTraincrewGameServiceのMock実装
/// </summary>
public sealed class MockTraincrewGameService : ITraincrewGameService
{
    private GameState _currentGameState = new();

    public bool IsConnected { get; private set; }

    public event EventHandler<GameState>? GameStateChanged;

    public Task ConnectAsync()
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<GameState> GetCurrentGameStateAsync()
    {
        return Task.FromResult(_currentGameState);
    }

    public Task UpdateGameStateAsync()
    {
        GameStateChanged?.Invoke(this, _currentGameState);
        return Task.CompletedTask;
    }

    public GameState GetCachedGameState()
    {
        return _currentGameState;
    }

    public void SetGameState(GameState state)
    {
        _currentGameState = state;
        GameStateChanged?.Invoke(this, _currentGameState);
    }
}
