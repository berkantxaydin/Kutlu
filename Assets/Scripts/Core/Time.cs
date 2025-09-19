using System;
using Cysharp.Threading.Tasks;

public interface ITurnService
{
    int CurrentTurn { get; }
    event Action<int> OnTurnStarted;
    event Action<int> OnTurnEnded;
    UniTask StartTurnsAsync(int turnDelayMs = 1000);
    void Stop();
    void Pause(); // Pause the turn sequence
    void Resume();
}

public class TurnService : ITurnService
{
    private readonly ICapitalRepository _capitalRepo;
    private readonly IResourceRepository _resourceRepo;

    public int CurrentTurn { get; private set; }
    private bool _isRunning;

    public event Action<int> OnTurnStarted;
    public event Action<int> OnTurnEnded;

    public TurnService(ICapitalRepository capitalRepo, IResourceRepository resourceRepo)
    {
        _capitalRepo = capitalRepo;
        _resourceRepo = resourceRepo;
        CurrentTurn = 0;
    }

    private bool _isPaused = false;

    public void Pause() => _isPaused = true;
    public void Resume() => _isPaused = false;

    public async UniTask StartTurnsAsync(int turnDelayMs = 1000)
    {
        _isRunning = true;

        while (_isRunning)
        {
            // --- Wait if paused ---
            while (_isPaused)
                await UniTask.Yield();

            CurrentTurn++;
            OnTurnStarted?.Invoke(CurrentTurn);

            // Production phase
            foreach (var capital in _capitalRepo.GetAll())
            {
                int produced = capital.Produce();
                var resource = _resourceRepo.GetByType(capital.ResourceType);
                resource?.Add(produced);
            }

            OnTurnEnded?.Invoke(CurrentTurn);

            await UniTask.Delay(turnDelayMs);
        }
    }


    public void Stop()
    {
        _isRunning = false;
    }

}
