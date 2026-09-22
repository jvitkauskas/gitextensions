using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using CommonTestUtils;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;
using GitUI.CommandsDialogs;
using GitUI.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.ScriptsEngine;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>
///  Exercises the hybrid process of the Avalonia port (docs/avalonia-port/PLAN.md, phase 1): Avalonia dialogs shown
///  modally over WinForms owners, WinForms dialogs shown over Avalonia dialogs, and real git operations.
/// </summary>
/// <remarks>
///  Screenshots of the real (not headless) windows are written to <c>&lt;test work dir&gt;/avalonia-hosting</c>.
/// </remarks>
// Avalonia binds to a single UI thread per process, hence one thread for all tests of this fixture.
[Apartment(ApartmentState.STA)]
[SingleThreaded]
public sealed class AvaloniaHostingTests
{
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int VK_ESCAPE = 0x1B;

    private ReferenceRepository _referenceRepository = null!;
    private GitUICommands _commands = null!;
    private Form _owner = null!;
    private Exception? _driveFailure;

    [SetUp]
    public void SetUp()
    {
        Environment.SetEnvironmentVariable(AvaloniaUi.EnvironmentVariable, "all");
        UserEnvironmentInformation.Initialise("0123456789012345678901234567890123456789", isDirty: false);

        _referenceRepository = new ReferenceRepository();
        _commands = new GitUICommands(GlobalServiceContainer.CreateDefaultMockServiceContainer(), _referenceRepository.Module);

        _owner = new Form { Text = "WinForms owner", Width = 900, Height = 600, StartPosition = FormStartPosition.CenterScreen };
        _owner.Show();
        Application.DoEvents();
    }

    [TearDown]
    public void TearDown()
    {
        Exception? driveFailure = _driveFailure;
        _driveFailure = null;
        AvaloniaDialogHost.DialogShowingForTests = null;
        _owner.Dispose();
        _referenceRepository.Dispose();
        Environment.SetEnvironmentVariable(AvaloniaUi.EnvironmentVariable, "none");

        if (driveFailure is not null)
        {
            throw new AssertionException($"Driving the dialog failed: {driveFailure}", driveFailure);
        }
    }

    [Test]
    public void About_is_modal_over_its_WinForms_owner()
    {
        bool ownerDisabledWhileOpen = false;
        string? productName = null;

        DriveNextDialog(window =>
        {
            ownerDisabledWhileOpen = !IsWindowEnabled(_owner.Handle);
            AboutViewModel viewModel = (AboutViewModel)window.DataContext!;
            productName = viewModel.ProductName;
            viewModel.ThanksToText.Should().StartWith("Thanks to over");
            Capture(window, "about");
            viewModel.CloseDialogCommand.Execute(null);
        });

        AvaloniaDialogs.TryShowAbout(_owner).Should().BeTrue();

        ownerDisabledWhileOpen.Should().BeTrue();
        IsWindowEnabled(_owner.Handle).Should().BeTrue("the owner is re-enabled when the dialog closes");
        productName.Should().Be(AppSettings.ApplicationName);
    }

    [Test]
    public void Escape_key_closes_the_dialog_without_saving()
    {
        int maxLineLength = AppSettings.CommitValidationMaxCntCharsPerLine;
        bool closed = false;

        DriveNextDialog(window =>
        {
            window.Closed += (_, _) => closed = true;
            ((CommitTemplateSettingsViewModel)window.DataContext!).MaxLineLength = maxLineLength + 1;

            // Real input path: a native key message processed by Avalonia inside the nested message loop.
            PostMessage(window.NativeHandle, WM_KEYDOWN, VK_ESCAPE, 1);
            PostMessage(window.NativeHandle, WM_KEYUP, VK_ESCAPE, unchecked((nint)0xC0000001));
        });

        AvaloniaDialogs.TryShowCommitTemplateSettings(_owner).Should().BeTrue();

        closed.Should().BeTrue();
        AppSettings.CommitValidationMaxCntCharsPerLine.Should().Be(maxLineLength);
    }

    [Test]
    public void Commit_template_settings_are_saved_to_AppSettings()
    {
        int originalMaxFirstLineLength = AppSettings.CommitValidationMaxCntCharsFirstLine;
        string? originalTemplates = AppSettings.CommitTemplates;
        try
        {
            DriveNextDialog(window =>
            {
                CommitTemplateSettingsViewModel viewModel = (CommitTemplateSettingsViewModel)window.DataContext!;
                viewModel.MaxFirstLineLength = 72;
                viewModel.SelectedTemplate = viewModel.Templates[1];
                viewModel.SelectedTemplate.Name = "Ticket";
                viewModel.SelectedTemplate.Text = "{{([A-Z]+-\\d+)}}: ";
                viewModel.SelectedTemplate.IsRegex = true;

                // Let the check box animation finish before capturing.
                DispatcherTimer.RunOnce(
                    () =>
                    {
                        Capture(window, "commit-template-settings");
                        viewModel.SaveCommand.Execute(null);
                    },
                    TimeSpan.FromMilliseconds(500));
            });

            AvaloniaDialogs.TryShowCommitTemplateSettings(_owner).Should().BeTrue();

            AppSettings.CommitValidationMaxCntCharsFirstLine.Should().Be(72);
            CommitTemplateItem[] templates = CommitTemplateItem.LoadFromSettings()!;
            templates.Should().HaveCount(CommitTemplateSettingsViewModel.TemplateSlotCount);
            templates[1].Name.Should().Be("Ticket");
            templates[1].IsRegex.Should().BeTrue();
        }
        finally
        {
            AppSettings.CommitValidationMaxCntCharsFirstLine = originalMaxFirstLineLength;
            AppSettings.CommitTemplates = originalTemplates!;
        }
    }

    [Test]
    public void StartRenameDialog_renames_the_branch_through_the_WinForms_progress_dialog()
    {
        _referenceRepository.CreateBranch("feature/old", _referenceRepository.CommitHash!);
        bool closeProcessDialog = AppSettings.CloseProcessDialog;
        AppSettings.CloseProcessDialog = true;
        try
        {
            DriveNextDialog(window =>
            {
                RenameBranchViewModel viewModel = (RenameBranchViewModel)window.DataContext!;
                viewModel.NewName.Should().Be("feature/old");
                viewModel.NewName = "feature/new";
                Capture(window, "rename-branch");

                // Runs `git branch -m` in the progress dialog (Avalonia too since phase 2, nested in this one).
                viewModel.RenameCommand.Execute(null);
            });

            bool renamed = _commands.StartRenameDialog(_owner, "feature/old");

            renamed.Should().BeTrue();
            IEnumerable<string> branches = _referenceRepository.Module.GetRefs(RefsFilter.Heads).Select(r => r.Name);
            branches.Should().Contain("feature/new").And.NotContain("feature/old");
        }
        finally
        {
            AppSettings.CloseProcessDialog = closeProcessDialog;
        }
    }

    [Test]
    public void Process_dialog_runs_git_and_shows_the_embedded_console()
    {
        bool closeProcessDialog = AppSettings.CloseProcessDialog;
        AppSettings.CloseProcessDialog = false;
        try
        {
            ProcessStatus? status = null;
            DriveNextDialog(window =>
            {
                ProcessViewModel viewModel = (ProcessViewModel)window.DataContext!;
                WhenDone(viewModel, () =>
                {
                    status = viewModel.Status;
                    viewModel.KeepDialogOpen.Should().BeTrue();
                    viewModel.AcknowledgeCommand.CanExecute(null).Should().BeTrue();
                    Capture(window, "process-success");
                    viewModel.AcknowledgeCommand.Execute(null);
                });
            });

            bool success = FormProcess.ShowDialog(_owner, _commands, arguments: "status", _referenceRepository.Module.WorkingDir, input: null, useDialogSettings: true, out string output);

            success.Should().BeTrue();
            status.Should().Be(ProcessStatus.Succeeded);
            output.Should().Contain("On branch");
        }
        finally
        {
            AppSettings.CloseProcessDialog = closeProcessDialog;
        }
    }

    [Test]
    public void Process_dialog_reports_a_failing_command()
    {
        DriveNextDialog(window =>
        {
            ProcessViewModel viewModel = (ProcessViewModel)window.DataContext!;
            WhenDone(viewModel, () =>
            {
                viewModel.Status.Should().Be(ProcessStatus.Failed);
                Capture(window, "process-failure");
                viewModel.AcknowledgeCommand.Execute(null);
            });
        });

        bool success = FormProcess.ShowDialog(_owner, _commands, arguments: "no-such-git-command", _referenceRepository.Module.WorkingDir, input: null, useDialogSettings: true, out string output);

        success.Should().BeFalse();
        output.Should().Contain("no-such-git-command");
    }

    [Test]
    public void Error_dialog_shows_the_given_output()
    {
        string? title = null;
        DriveNextDialog(window =>
        {
            ProcessViewModel viewModel = (ProcessViewModel)window.DataContext!;
            title = viewModel.Title;
            viewModel.IsDone.Should().BeTrue();
            viewModel.IsAbortVisible.Should().BeFalse();
            Capture(window, "error-dialog");
            viewModel.AcknowledgeCommand.Execute(null);
        });

        FormStatus.ShowErrorDialog(_owner, _commands, "Something failed", "first line\n", "second line\n");

        title.Should().Be("Something failed");
    }

    [Test]
    public void StartAddFilesDialog_adds_files_through_the_progress_dialog()
    {
        File.WriteAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "new-file.txt"), "content");

        DriveDialogs(
            window =>
            {
                AddFilesViewModel viewModel = (AddFilesViewModel)window.DataContext!;
                viewModel.Filter = "new-file.txt";
                Capture(window, "add-files");
                viewModel.AddFilesCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        _commands.StartAddFilesDialog(_owner).Should().BeTrue();

        _referenceRepository.Module.GetIndexFiles().Select(f => f.Name).Should().Contain("new-file.txt");
    }

    [Test]
    public void StartDeleteTagDialog_deletes_the_tag()
    {
        _referenceRepository.CreateTag("v1.0", _referenceRepository.CommitHash!);

        DriveNextDialog(window =>
        {
            DeleteTagViewModel viewModel = (DeleteTagViewModel)window.DataContext!;
            viewModel.TagName.Should().Be("v1.0");
            viewModel.Tags.Should().Contain("v1.0");
            Capture(window, "delete-tag");
            viewModel.DeleteCommand.Execute(null);
        });

        _commands.StartDeleteTagDialog(_owner, "v1.0").Should().BeTrue();

        _referenceRepository.Module.GetRefs(RefsFilter.Tags).Should().BeEmpty();
    }

    [Test]
    public void StartInitializeDialog_creates_a_repository()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ge-avalonia-init-{Guid.NewGuid():N}");
        GitModuleEventArgs? created = null;
        try
        {
            DriveNextDialog(window =>
            {
                InitViewModel viewModel = (InitViewModel)window.DataContext!;
                viewModel.Directory = directory;
                Capture(window, "init");

                // Git's output is reported in a (WinForms) message box; dismiss it.
                DispatcherTimer.RunOnce(() => CloseTopLevelWindow("Create new repository", except: window.NativeHandle), TimeSpan.FromMilliseconds(1500));
                viewModel.CreateCommand.Execute(null);
            });

            _commands.StartInitializeDialog(_owner, dir: null, (_, e) => created = e).Should().BeTrue();

            Directory.Exists(Path.Combine(directory, ".git")).Should().BeTrue();
            created!.GitModule.WorkingDir.Should().Be(directory.EnsureTrailingPathSeparator());
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public void ShowResetDialog_returns_the_choice()
    {
        DriveNextDialog(window =>
        {
            ResetChangesViewModel viewModel = (ResetChangesViewModel)window.DataContext!;
            viewModel.CanChooseDeleteNewFiles.Should().BeTrue();
            Capture(window, "reset-changes");
            viewModel.DeleteNewFiles = true;
            viewModel.ResetCommand.Execute(null);
        });

        FormResetChanges.ShowResetDialog(_owner, hasExistingFiles: true, hasNewFiles: true)
            .Should().Be(FormResetChanges.ActionEnum.ResetAndDelete);
    }

    [Test]
    public void Script_input_prompt_returns_the_input()
    {
        DriveNextDialog(window =>
        {
            SimplePromptViewModel viewModel = (SimplePromptViewModel)window.DataContext!;
            viewModel.Input = "ABC-123";
            Capture(window, "simple-prompt");
            viewModel.OkCommand.Execute(null);
        });

        using GitUI.ScriptsEngine.IUserInputPrompt prompt = new GitUI.ScriptsEngine.SimplePromptCreator().Create("Script", "Ticket", "");

        prompt.ShowDialog(_owner).Should().Be(DialogResult.OK);
        prompt.UserInput.Should().Be("ABC-123");
    }

    [Test]
    public void Small_dialogs_open_from_their_entry_points()
    {
        DriveNextDialog(window =>
        {
            ((CommandlineHelpViewModel)window.DataContext!).Commands.Should().Contain("browse");
            Capture(window, "commandline-help");
            window.Close();
        });
        AvaloniaDialogs.TryShowCommandlineHelp().Should().BeTrue();

        DriveNextDialog(window =>
        {
            GoToLineViewModel viewModel = (GoToLineViewModel)window.DataContext!;
            viewModel.LineNumber = 7;
            viewModel.OkCommand.Execute(null);
        });
        AvaloniaDialogs.TryShowGoToLine(_owner, 100, out int? line).Should().BeTrue();
        line.Should().Be(7);

        DriveNextDialog(window =>
        {
            Capture(window, "contributors");
            window.Close();
        });
        AvaloniaDialogs.TryShowContributors(_owner).Should().BeTrue();
    }

    private void AcknowledgeWhenDone(DialogWindow window)
    {
        ProcessViewModel viewModel = (ProcessViewModel)window.DataContext!;
        WhenDone(viewModel, () => viewModel.AcknowledgeCommand.Execute(null));
    }

    /// <summary>Closes the top-level window of this thread with the given title (e.g. a message box).</summary>
    private static void CloseTopLevelWindow(string title, nint except)
    {
        EnumThreadWindows(
            GetCurrentThreadId(),
            (handle, _) =>
            {
                System.Text.StringBuilder text = new(256);
                GetWindowText(handle, text, text.Capacity);
                if (handle != except && text.ToString() == title)
                {
                    PostMessage(handle, 0x0010 /* WM_CLOSE */, 0, 0);
                }

                return true;
            },
            0);
    }

    private delegate bool EnumWindowsProc(nint handle, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint handle, System.Text.StringBuilder text, int maxCount);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    /// <summary>Runs <paramref name="action"/> once the process of the dialog has finished (and the UI settled).</summary>
    private void WhenDone(ProcessViewModel viewModel, Action action)
    {
        if (viewModel.IsDone)
        {
            DispatcherTimer.RunOnce(Guarded, TimeSpan.FromMilliseconds(300));
            return;
        }

        void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProcessViewModel.IsDone) && viewModel.IsDone)
            {
                viewModel.PropertyChanged -= OnPropertyChanged;
                DispatcherTimer.RunOnce(Guarded, TimeSpan.FromMilliseconds(300));
            }
        }

        viewModel.PropertyChanged += OnPropertyChanged;

        void Guarded()
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _driveFailure = ex;
                viewModel.AcknowledgeCommand.Execute(null);
            }
        }
    }

    /// <summary>
    ///  Runs <paramref name="drive"/> once the next Avalonia dialog has been shown and rendered.
    ///  The dialog is modal, so this is the only way to act on it while the <c>TryShow*</c> call blocks.
    /// </summary>
    private void DriveNextDialog(Action<DialogWindow> drive) => DriveDialogs(drive);

    /// <summary>Drives the next Avalonia dialogs in order, e.g. a dialog and the progress dialog it opens.</summary>
    private void DriveDialogs(params Action<DialogWindow>[] drivers)
    {
        Queue<Action<DialogWindow>> queue = new(drivers);
        AvaloniaDialogHost.DialogShowingForTests = window =>
        {
            Action<DialogWindow> drive = queue.Dequeue();
            if (queue.Count == 0)
            {
                AvaloniaDialogHost.DialogShowingForTests = null;
            }

            window.Opened += (_, _) => DispatcherTimer.RunOnce(
                () =>
                {
                    try
                    {
                        drive(window);
                    }
                    catch (Exception ex)
                    {
                        _driveFailure = ex;
                        window.Close();
                    }
                },
                TimeSpan.FromMilliseconds(500));
        };
    }

    private static void Capture(DialogWindow window, string name)
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "avalonia-hosting");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"{name}.png");

        GetWindowRect(window.NativeHandle, out RECT rect);
        using Bitmap bitmap = new(rect.Right - rect.Left, rect.Bottom - rect.Top);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            nint hdc = graphics.GetHdc();
            PrintWindow(window.NativeHandle, hdc, 2 /* PW_RENDERFULLCONTENT */);
            graphics.ReleaseHdc(hdc);
        }

        bitmap.Save(path, ImageFormat.Png);
        TestContext.AddTestAttachment(path);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint handle, int msg, nint wordParameter, nint longParameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint handle, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(nint handle, nint deviceContext, uint flags);
}
