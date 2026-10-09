namespace EvTrainer.Web.State;

/// <summary>Bottom toast with an optional "Desfazer" action; hides itself after 4 s.</summary>
public sealed class ToastService(TimeProvider time) : IDisposable
{
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(4);

    private ITimer? _timer;

    public event Action? Changed;

    public string? Message { get; private set; }

    public Action? Undo { get; private set; }

    public void Show(string message, Action? undo = null)
    {
        Message = message;
        Undo = undo;
        _timer?.Dispose();
        _timer = time.CreateTimer(_ => Hide(), null, Duration, Timeout.InfiniteTimeSpan);
        Changed?.Invoke();
    }

    public void RunUndo()
    {
        var undo = Undo;
        Hide();
        undo?.Invoke();
    }

    public void Hide()
    {
        _timer?.Dispose();
        _timer = null;
        Message = null;
        Undo = null;
        Changed?.Invoke();
    }

    public void Dispose() => _timer?.Dispose();
}