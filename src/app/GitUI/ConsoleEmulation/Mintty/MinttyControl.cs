using System.Diagnostics;
using System.Text;
using GitExtensions.Extensibility;
using Microsoft.VisualStudio.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace GitUI.ConsoleEmulation.Mintty;

internal sealed class MinttyControl : IDisposable
{
    private readonly NativeHostWindow _host;
    private MinttySession? _runningSession;
    private CancellationTokenSource? _sessionCts;
    private HANDLE _jobHandle = NativeMethods.CreateKillOnCloseJob();
    private bool _isDisposed;

    /// <param name="host">The window in which mintty runs (in place of the WinForms panel), owned by the caller.</param>
    public MinttyControl(NativeHostWindow host)
    {
        _host = host;

        // The host window forwards the focus to the embedded mintty hwnd whenever it gets it (e.g. after
        // alt-tabbing back to the host window) — centralising what used to be scattered
        // FocusWindowWithAttachedInput calls at every entry point.
        _host.FocusReceived += OnGotFocus;
        _host.Resized += OnResize;
    }

    internal MinttySession? RunningSession => _runningSession;

    internal bool IsShellRunning => _runningSession is not null && !_runningSession.IsExited;

    public MinttySession StartCommand(MinttyStartInfo startInfo, string minttyPath, string bashPath, ConsoleEmulatorSettings consoleSettings)
    {
        ResetSession();

        CancellationTokenSource sessionCts = _sessionCts!;
        CancellationToken ct = sessionCts.Token;

        (MinttySession session, MinttyConsoleRuntime.CommandLaunchParams launchParams) = MinttySession.StartCommandSession(startInfo);
        _runningSession = session;

        string minttyArgs = $"{BuildMinttyThemingArgs(consoleSettings)}--nodaemon --window hide --log - \"{bashPath}\" -c \"{launchParams.BashBootstrapCommand}\"";

        LaunchAndEmbed(session, minttyPath, minttyArgs, startInfo.StartupDirectory, launchParams.EnvironmentVariables, OnExited, ct, redirectStdout: true);

        return session;

        void OnExited()
        {
            // If ResetSession already cancelled this CTS, skip the callback —
            // the old session was intentionally killed, not a host termination.
            if (!sessionCts.IsCancellationRequested)
            {
                sessionCts.Cancel();
                startInfo.ConsoleClosedCallback?.Invoke();
            }
        }
    }

    public MinttySession StartInteractiveShell(string minttyPath, string bashPath, string workDir, ConsoleEmulatorSettings consoleSettings, Action? shellExitedCallback = null)
    {
        ResetSession();
        CancellationTokenSource sessionCts = _sessionCts!;
        CancellationToken ct = sessionCts.Token;

        string minttyArgs = $"{BuildMinttyThemingArgs(consoleSettings)}--nodaemon --window hide \"{bashPath}\" --login -i";

        MinttySession session = MinttySession.StartInteractiveShellSession();
        _runningSession = session;

        LaunchAndEmbed(session, minttyPath, minttyArgs, workDir, new Dictionary<string, string>(), OnExited, ct);

        return session;

        void OnExited()
        {
            // If ResetSession already cancelled this CTS, skip the callback —
            // the old session was intentionally killed, not a host termination.
            if (!sessionCts.IsCancellationRequested)
            {
                sessionCts.Cancel();
                shellExitedCallback?.Invoke();
            }
        }
    }

    public void SendConsoleInput(string text)
    {
        HWND hwnd = _runningSession?.WindowHandle ?? HWND.Null;
        if (hwnd.IsNull)
        {
            return;
        }

        NativeMethods.FocusWindowWithAttachedInput(hwnd);

        foreach (char c in text)
        {
            if (c is '\r' or '\n')
            {
                PInvoke.PostMessage(hwnd, NativeMethods.WM_KEYDOWN, (nuint)VIRTUAL_KEY.VK_RETURN, (nint)0);
                PInvoke.PostMessage(hwnd, NativeMethods.WM_CHAR, (nuint)'\r', (nint)0);
                PInvoke.PostMessage(hwnd, NativeMethods.WM_KEYUP, (nuint)VIRTUAL_KEY.VK_RETURN, (nint)0);
            }
            else
            {
                PInvoke.PostMessage(hwnd, NativeMethods.WM_CHAR, (nuint)c, (nint)0);
            }
        }
    }

    private static string BuildMinttyThemingArgs(ConsoleEmulatorSettings consoleSettings)
    {
        StringBuilder args = new();
        if (!string.IsNullOrEmpty(consoleSettings.Theme))
        {
            args.Append($"-o \"ThemeFile={consoleSettings.Theme}\" ");
        }

        if (consoleSettings.Font is { } font)
        {
            args.Append($"-o \"Font={font.FamilyName}\" -o \"FontHeight={(int)(font.SizeInPoints + 0.5f)}\" ");
        }

        return args.ToString();
    }

    private void ResetSession()
    {
        _sessionCts?.Cancel();
        _sessionCts?.Dispose();
        _sessionCts = new CancellationTokenSource();
        _runningSession?.Dispose();
        _runningSession = null;
    }

    private void LaunchAndEmbed(MinttySession session,
        string minttyPath,
        string minttyArgs,
        string? workDir,
        IReadOnlyDictionary<string, string> envVariables,
        Action onExited,
        CancellationToken ct,
        bool redirectStdout = false)
    {
        ProcessStartInfo psi = new()
        {
            FileName = minttyPath,
            Arguments = minttyArgs,
            WorkingDirectory = workDir ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = redirectStdout,
            CreateNoWindow = true,
        };

        foreach ((string key, string value) in envVariables)
        {
            psi.EnvironmentVariables[key] = value;
        }

        Process minttyProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start mintty.exe");

        if (!_jobHandle.IsNull)
        {
            PInvoke.AssignProcessToJobObject(_jobHandle, new HANDLE(minttyProcess.Handle));
        }

        // Subscribe before enabling events so a near-instant exit (e.g. mintty fails to start)
        // can't fire and be lost before the handler is attached.
        minttyProcess.Exited += (_, _) => onExited();
        minttyProcess.EnableRaisingEvents = true;

        session.AttachProcess(minttyProcess);

        ThreadHelper.FileAndForget(() => FindAndEmbedWindowAsync(minttyProcess, session, ct));
    }

    private async Task FindAndEmbedWindowAsync(Process minttyProcess, MinttySession session, CancellationToken sessionCt)
    {
        ThreadHelper.ThrowIfOnUIThread();

        TimeSpan findWindowTimeout = TimeSpan.FromSeconds(15);
        using CancellationTokenSource timeoutCts = new(findWindowTimeout);
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCt, timeoutCts.Token);
        CancellationToken ct = linkedCts.Token;

        try
        {
            // Wait until mintty's message pump is idle before embedding — otherwise
            // mintty may still be running startup code that calls ShowWindow/SetFocus
            // and causes the host dialog's focus to flicker as it races with our embed.
            // If the wait times out under load, proceed anyway: the embed may still
            // succeed, and killing mintty here would also kill the spawned git process.
            TimeSpan idleTimeout = TimeSpan.FromSeconds(10);
            bool idle = await Task.Run(() => minttyProcess.WaitForInputIdle(idleTimeout), ct);
            if (!idle)
            {
                throw new InvalidOperationException($"Waiting for mintty to get idle timed out after {idleTimeout.TotalSeconds} seconds for PID {minttyProcess.Id}.");
            }

            HWND hwnd = HWND.Null;

            while (!ct.IsCancellationRequested)
            {
                hwnd = NativeMethods.FindMinttyWindowForProcess(minttyProcess.Id);
                if (!hwnd.IsNull)
                {
                    break;
                }

                // It is essential to capture original synchronization context
                await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
            }

            if (hwnd.IsNull)
            {
                throw new InvalidOperationException($"Could not locate mintty window for PID {minttyProcess.Id} within {findWindowTimeout.TotalSeconds} seconds.");
            }

            await EmbedWindowAsync(hwnd);

            // Set WindowHandle only after the embed completes so OnResize doesn't
            // post a SetWindowPos to mintty mid-reparent.
            session.WindowHandle = hwnd;
        }
        catch (OperationCanceledException) when (sessionCt.IsCancellationRequested)
        {
            // Session was reset/disposed while we were waiting — no error to surface.
        }
        catch (Exception ex)
        {
            // Don't kill mintty: the spawned git process is in the same job and would be
            // terminated mid-write, leaving traces like index.lock behind. Let the command finish in
            // the hidden mintty window — its output still reaches the host via stdout.
            Trace.WriteLine($"MinttyControl: Failed to embed mintty window for PID {minttyProcess.Id}. The command will continue running invisibly until completion. Error: {ex}");
            ThreadHelper.InvokeAndForget(() => ShowEmbedFailure(ex, minttyProcess));
        }
    }

    private async Task EmbedWindowAsync(HWND hwnd)
    {
        // Capture the state of the host window on the UI thread.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        HWND panelHwnd = (HWND)_host.Handle;
        (int width, int height) = _host.ClientSize;
        await TaskScheduler.Default;

        // The synchronous cross-process Win32 calls below all send messages to
        // mintty's thread. Running them on the UI thread freezes the entire app
        // when mintty's pump stalls.
        ThreadHelper.ThrowIfOnUIThread();

        // Probe mintty's pump before any blocking cross-process call.
        // SendMessageTimeout(SMTO_ABORTIFHUNG) is bounded and cannot itself
        // hang. If this fails, none of the cross-process calls below are safe.
        TimeSpan responsivenessTimeout = TimeSpan.FromSeconds(5);
        if (!NativeMethods.IsWindowResponsive(hwnd, responsivenessTimeout))
        {
            throw new InvalidOperationException($"Mintty window did not respond within {responsivenessTimeout.TotalSeconds} seconds.");
        }

        WINDOW_STYLE style = (WINDOW_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        style &= ~(WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_BORDER);
        style |= WINDOW_STYLE.WS_CHILD;
        PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)style);

        WINDOW_EX_STYLE extendedStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        extendedStyle &= ~(WINDOW_EX_STYLE.WS_EX_CLIENTEDGE | WINDOW_EX_STYLE.WS_EX_WINDOWEDGE | WINDOW_EX_STYLE.WS_EX_APPWINDOW);
        extendedStyle |= WINDOW_EX_STYLE.WS_EX_TOOLWINDOW;
        PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)extendedStyle);

        PInvoke.SetParent(hwnd, panelHwnd);

        // SWP_ASYNCWINDOWPOS posts the size/show change to mintty's thread instead
        // of a synchronous cross-process SendMessage. SWP_SHOWWINDOW combined with
        // SWP_NOACTIVATE is equivalent to SW_SHOWNA. See OnResize for the same pattern.
        PInvoke.SetWindowPos(hwnd, HWND.Null, 0, 0, width, height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER
            | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED
            | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
            | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW
            | SET_WINDOW_POS_FLAGS.SWP_ASYNCWINDOWPOS);

        // Defer Focus via InvokeAndForget so our pump gets one cycle to drain. Mintty
        // is now parented to our window — that means cross-process messages
        // (WM_PARENTNOTIFY, WM_MOUSEACTIVATE, focus-traversal traffic, etc.) can
        // flow synchronously from mintty into our pump. Anything mintty posts
        // while our continuation is dispatched gets handled before our queued
        // Focus runs, instead of being processed reentrantly underneath SetFocus.
        ThreadHelper.InvokeAndForget(Focus);
    }

    /// <summary>Gives the focus to the host window, which forwards it to mintty.</summary>
    public void Focus() => _host.Focus();

    private void OnGotFocus(object? sender, EventArgs e)
    {
        HWND hwnd = _runningSession?.WindowHandle ?? HWND.Null;
        if (!hwnd.IsNull)
        {
            NativeMethods.FocusWindowWithAttachedInput(hwnd);
        }
    }

    private void OnResize(object? sender, EventArgs e)
    {
        HWND hwnd = _runningSession?.WindowHandle ?? HWND.Null;
        (int width, int height) = _host.ClientSize;
        if (!hwnd.IsNull && width > 0 && height > 0)
        {
            // SWP_ASYNCWINDOWPOS posts the size change to mintty's thread instead of a
            // synchronous cross-process SendMessage. MoveWindow (and plain SetWindowPos)
            // blocks on mintty's message pump, which can deadlock with the modal resize
            // loop if mintty has a SendMessage pending into our window.
            PInvoke.SetWindowPos(hwnd, HWND.Null, 0, 0, width, height,
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
                | SET_WINDOW_POS_FLAGS.SWP_ASYNCWINDOWPOS);
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _host.FocusReceived -= OnGotFocus;
            _host.Resized -= OnResize;
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            _sessionCts = null;
            _runningSession?.Dispose();
            _runningSession = null;

            if (!_jobHandle.IsNull)
            {
                try
                {
                    PInvoke.TerminateJobObject(_jobHandle, 0);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }

                PInvoke.CloseHandle(_jobHandle);
                _jobHandle = HANDLE.Null;
            }
        }
    }

    private void ShowEmbedFailure(Exception ex, Process minttyProcess)
    {
        if (_isDisposed)
        {
            return;
        }

        // ReSharper disable LocalizableElement - rare error, no need to localize
        TaskDialogPage page = new()
        {
            Icon = TaskDialogIcon.Warning,
            Caption = "mintty",
            Heading = "Failed to display the embedded console",
            Text = ex.Message,
            AllowCancel = true,
            SizeToContent = true,
        };
        TaskDialogCommandLinkButton showButton = new("Show mintty window externally");

        // ReSharper restore LocalizableElement
        showButton.Click += (_, _) =>
        {
            try
            {
                if (minttyProcess.HasExited)
                {
                    return;
                }

                // Try the targeted lookup first; only fall back to enumerating every top-level window of the process
                // if it fails (e.g. mintty's pump was so slow we never saw its window appear).
                HWND mainHwnd = NativeMethods.FindMinttyWindowForProcess(minttyProcess.Id);
                List<HWND> windows = mainHwnd.IsNull
                    ? NativeMethods.EnumerateProcessWindows(minttyProcess.Id)
                    : [mainHwnd];

                foreach (HWND hwnd in windows)
                {
                    // mintty was started with --window hide; if embedding failed, it threw
                    // before we touched any styles, so the window keeps its original chrome
                    // and just needs to be unhidden.
                    PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
                }
            }
            catch (Exception clickEx)
            {
                Trace.WriteLine($"MinttyControl: Failed to surface mintty window externally: {clickEx}");
            }
        };

        page.Buttons.Add(showButton);
        page.Buttons.Add(TaskDialogButton.Close);
        TaskDialog.ShowDialog(_host.Handle, page);
    }
}
