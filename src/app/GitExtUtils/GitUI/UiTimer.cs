namespace GitExtUtils.GitUI;

/// <summary>
///  A timer whose <see cref="Tick"/> is raised on the thread that created it, through its synchronization context (the UI
///  thread), as the WinForms <c>System.Windows.Forms.Timer</c>.
/// </summary>
public sealed class UiTimer : IDisposable
{
    private readonly SynchronizationContext? _context = SynchronizationContext.Current;
    private readonly Timer _timer;
    private int _interval = 100;
    private bool _enabled;
    private bool _isDisposed;
    private int _generation;
    private int _tickPending;

    public UiTimer()
    {
        _timer = new Timer(OnElapsed);
    }

    /// <summary>Raised every <see cref="Interval"/> milliseconds while <see cref="Enabled"/>.</summary>
    public event EventHandler? Tick;

    /// <summary>The time between the ticks, in milliseconds.</summary>
    public int Interval
    {
        get => _interval;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _interval = value;
            if (_enabled)
            {
                Restart();
            }
        }
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (value == _enabled || _isDisposed)
            {
                return;
            }

            _enabled = value;
            if (value)
            {
                Restart();
            }
            else
            {
                _generation++;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }

    public void Start() => Enabled = true;

    public void Stop() => Enabled = false;

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _enabled = false;
            _generation++;
            _timer.Dispose();
        }
    }

    private void Restart()
    {
        _generation++;
        _timer.Change(_interval, _interval);
    }

    private void OnElapsed(object? state)
    {
        // As WM_TIMER: the ticks are not queued while one is waiting for the UI thread.
        if (Interlocked.Exchange(ref _tickPending, 1) == 1)
        {
            return;
        }

        int generation = _generation;
        void Raise(object? unused)
        {
            Volatile.Write(ref _tickPending, 0);

            // A tick of the timer before it was stopped or restarted is dropped.
            if (_enabled && !_isDisposed && generation == _generation)
            {
                Tick?.Invoke(this, EventArgs.Empty);
            }
        }

        if (_context is not null)
        {
#pragma warning disable VSTHRD001 // As a WinForms timer: posted to the context of the thread that created it, which may have no JoinableTaskContext.
            _context.Post(Raise, null);
#pragma warning restore VSTHRD001
        }
        else
        {
            Raise(null);
        }
    }
}
