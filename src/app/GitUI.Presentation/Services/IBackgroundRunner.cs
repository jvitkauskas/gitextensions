namespace GitUI.Presentation.Services;

/// <summary>
///  Runs the long work of a view model off the UI thread and brings results back to it (JoinableTaskFactory in the app,
///  synchronously in tests).
/// </summary>
public interface IBackgroundRunner
{
    /// <summary>Runs <paramref name="work"/> on a background thread; the returned task completes on the UI thread.</summary>
    Task<T> RunAsync<T>(Func<T> work, CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="action"/> on the UI thread; may be called from any thread.</summary>
    void Post(Action action);
}
