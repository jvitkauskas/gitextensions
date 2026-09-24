using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Presentation;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.FindLargeFiles;

/// <summary>Strings of the Avalonia port of <see cref="FindLargeFilesForm"/>; ids match the form.</summary>
public sealed class FindLargeFilesStrings : ViewStrings
{
    public FindLargeFilesStrings()
        : base("FindLargeFilesForm")
    {
        Title = Add("$this", "Text", "Find large files");
        Instructions = Add("label1", "Text", "Reset local changes before deleting files. Choose files to delete. Force push for replacing data on remote repository.");
        ShaColumn = Add("sHADataGridViewTextBoxColumn", "HeaderText", "SHA");
        PathColumn = Add("pathDataGridViewTextBoxColumn", "HeaderText", "Path");
        SizeColumn = Add("sizeDataGridViewTextBoxColumn", "HeaderText", "Size");
        CompressedSizeColumn = Add("CompressedSize", "HeaderText", "Compressed size");
        CommitCountColumn = Add("commitCountDataGridViewTextBoxColumn", "HeaderText", "Commit count");
        LastCommitDateColumn = Add("lastCommitDateDataGridViewTextBoxColumn", "HeaderText", "Last commit date");
        DeleteColumn = Add("dataGridViewCheckBoxColumn1", "HeaderText", "Delete");
        Delete = Add("Delete", "Text", "Delete");
        Close = Add("Cancel", "Text", "Close");
        AreYouSureToDelete = Add("_areYouSureToDelete", "Text", "Are you sure to delete the selected files?");
        DeleteCaption = Add("_deleteCaption", "Text", "Delete");
    }

    public TranslatedText Title { get; }

    public TranslatedText Instructions { get; }

    public TranslatedText ShaColumn { get; }

    public TranslatedText PathColumn { get; }

    public TranslatedText SizeColumn { get; }

    public TranslatedText CompressedSizeColumn { get; }

    public TranslatedText CommitCountColumn { get; }

    public TranslatedText LastCommitDateColumn { get; }

    public TranslatedText DeleteColumn { get; }

    public TranslatedText Delete { get; }

    public TranslatedText Close { get; }

    public TranslatedText AreYouSureToDelete { get; }

    public TranslatedText DeleteCaption { get; }
}

/// <summary>A row of the list of large files, a <see cref="GitObject"/> whose data is updated while searching.</summary>
public sealed class GitObjectRow : ObservableObject
{
    public GitObjectRow(GitObject gitObject)
    {
        Object = gitObject;
    }

    public GitObject Object { get; }

    public string Sha => Object.SHA;

    public string Path => Object.Path;

    public string Size => Object.Size;

    /// <summary>The sort key of <see cref="Size"/> (as <c>SortableObjectsList</c>).</summary>
    public int SizeInBytes => Object.SizeInBytes;

    public string CompressedSize => Object.CompressedSize;

    /// <summary>The sort key of <see cref="CompressedSize"/>.</summary>
    public int CompressedSizeInBytes => Object.CompressedSizeInBytes;

    public int CommitCount => Object.CommitCount;

    public DateTime LastCommitDate => Object.LastCommitDate;

    public bool Delete
    {
        get => Object.Delete;
        set => SetProperty(Object.Delete, value, Object, (gitObject, delete) => gitObject.Delete = delete);
    }

    /// <summary>As <c>ResetItem</c> of the binding list: the data of the object changed.</summary>
    public void Refresh() => OnPropertyChanged(string.Empty);
}

/// <summary>View model of the Avalonia port of <see cref="FindLargeFilesForm"/>.</summary>
public sealed partial class FindLargeFilesViewModel : DialogViewModel
{
    private readonly float _threshold;
    private readonly IGitModule _module;
    private readonly string _gitCommand;
    private readonly IBackgroundRunner _backgroundRunner;
    private readonly IMessageBoxService _messageBoxes;
    private readonly Action<string> _startBatchFileProcess;
    private readonly Dictionary<string, GitObjectRow> _rows = [];
    private string[] _revList = [];

    /// <param name="threshold">The size from which files are listed, in megabytes (the setting of the plugin).</param>
    /// <param name="gitCommand">The git executable of the generated batch file (<c>AppSettings.GitCommand</c>).</param>
    /// <param name="startBatchFileProcess">Runs the batch file in the progress dialog (<c>StartBatchFileProcessDialog</c>).</param>
    public FindLargeFilesViewModel(
        FindLargeFilesStrings strings,
        float threshold,
        IGitModule module,
        string gitCommand,
        IBackgroundRunner backgroundRunner,
        IMessageBoxService messageBoxes,
        Action<string> startBatchFileProcess)
    {
        Strings = strings;
        _threshold = threshold;
        _module = module;
        _gitCommand = gitCommand;
        _backgroundRunner = backgroundRunner;
        _messageBoxes = messageBoxes;
        _startBatchFileProcess = startBatchFileProcess;
    }

    public FindLargeFilesStrings Strings { get; }

    public ObservableCollection<GitObjectRow> GitObjects { get; } = [];

    /// <summary>Whether the search runs: the progress bar is shown and the check boxes are read-only.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    public partial bool IsSearching { get; private set; }

    public bool IsEditable => !IsSearching;

    [ObservableProperty]
    public partial int ProgressMaximum { get; private set; } = 1;

    [ObservableProperty]
    public partial int ProgressValue { get; private set; }

    /// <summary>As <c>OnLoad</c>: lists the revisions of HEAD, then searches their large files in the background.</summary>
    public async Task SearchAsync()
    {
        GitArgumentBuilder args = new("rev-list") { "HEAD" };
        _revList = _module.GitExecutable.GetOutput(args).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        ProgressMaximum = Math.Max(1, (int)(_revList.Length * 1.1f));
        IsSearching = true;
        await _backgroundRunner.RunAsync(FindLargeFiles);
        IsSearching = false;
    }

    /// <summary>As <c>Delete_Click</c>: runs a batch file that rewrites the history without the checked files.</summary>
    [RelayCommand]
    private void Delete()
    {
        if (_messageBoxes.Confirm(Strings.AreYouSureToDelete.Text, Strings.DeleteCaption.Text))
        {
            _startBatchFileProcess(GenerateCommand(_gitCommand, GitObjects.Select(row => row.Object)));
        }

        Close(accepted: false);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);

    /// <summary>As <c>FindLargeFilesForm.GenerateCommand</c> (compared with it by <c>FindLargeFilesViewModelTests</c>).</summary>
    public static string GenerateCommand(string gitCommand, IEnumerable<GitObject> gitObjects)
    {
        StringBuilder sb = new();
        sb.AppendLine($"SET gitexe=\"{gitCommand}\"");

        foreach (GitObject gitObject in gitObjects.Where(gitObject => gitObject.Delete))
        {
            sb.AppendLine($"%gitexe% filter-branch --index-filter \"git rm -r -f --cached --ignore-unmatch '{gitObject.Path}'\" --prune-empty -- --all");
        }

        sb.AppendLine("for /f \"usebackq\" %%a IN (`\"%gitexe% for-each-ref --format=\"%%^(refname^)\" refs/original/\"`) DO %gitexe% update-ref -d %%a");
        sb.AppendLine("%gitexe% reflog expire --expire=now --all");
        sb.AppendLine("%gitexe% gc --aggressive --prune=now");

        return sb.ToString();
    }

    /// <summary>As <c>FindLargeFilesFunction</c>; runs in the background and posts the rows and the progress to the UI thread.</summary>
    private bool FindLargeFiles()
    {
        try
        {
            Dictionary<string, DateTime> revData = [];
            foreach (GitObject d in GetLargeFiles(_threshold))
            {
                string commit = d.Commit.First();
                if (!revData.TryGetValue(commit, out DateTime date))
                {
                    GitArgumentBuilder args = new("show")
                    {
                        "-s",
                        commit,
                        "--format=\"%ci\""
                    };
                    string revDate = _module.GitExecutable.GetOutput(args);
                    if (!DateTime.TryParse(revDate, out date))
                    {
                        Trace.WriteLine($"Could not parse date '{revDate}' for commit '{commit}'");
                        date = DateTime.MinValue;
                    }

                    revData.Add(commit, date);
                }

                if (!_rows.TryGetValue(d.SHA, out GitObjectRow? row))
                {
                    d.LastCommitDate = date;
                    GitObjectRow newRow = new(d);
                    _rows.Add(d.SHA, newRow);
                    _backgroundRunner.Post(() => GitObjects.Add(newRow));
                }
                else if (!row.Object.Commit.Contains(commit))
                {
                    if (row.Object.LastCommitDate < date)
                    {
                        row.Object.LastCommitDate = date;
                    }

                    row.Object.Commit.Add(commit);
                    _backgroundRunner.Post(row.Refresh);
                }
            }

            string objectsPackDirectory = _module.ResolveGitInternalPath("objects/pack/");
            if (Directory.Exists(objectsPackDirectory))
            {
                string[] packFiles = Directory.GetFiles(objectsPackDirectory, "pack-*.idx");
                foreach (string pack in packFiles)
                {
                    GitArgumentBuilder args = new("verify-pack")
                    {
                        "-v",
                        pack
                    };

                    string[] objects = _module.GitExecutable.GetOutput(args).Split('\n');
                    int progressStep = (int)((_revList.Length * 0.1f) / packFiles.Length);
                    _backgroundRunner.Post(() => ProgressValue = Math.Min(ProgressMaximum, ProgressValue + progressStep));
                    foreach (string gitObject in objects.Where(x => x.Contains(" blob ")))
                    {
                        string[] dataFields = gitObject.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                        if (_rows.TryGetValue(dataFields[0], out GitObjectRow? row) && int.TryParse(dataFields[3], out int compressedSize))
                        {
                            row.Object.CompressedSizeInBytes = compressedSize;
                            _backgroundRunner.Post(row.Refresh);
                        }
                    }
                }
            }
        }
        catch
        {
            // As the WinForms form: the search stops at the first error.
        }

        return true;
    }

    /// <summary>As <c>GetLargeFiles</c>.</summary>
    private IEnumerable<GitObject> GetLargeFiles(float threshold)
    {
        int thresholdSize = (int)(threshold * 1024 * 1024);
        for (int i = 0; i < _revList.Length; i++)
        {
            int progress = i;
            _backgroundRunner.Post(() => ProgressValue = progress);
            string rev = _revList[i];
            GitArgumentBuilder args = new("ls-tree")
            {
                "-zrl",
                rev.Quote()
            };
            string[] objects = _module.GitExecutable.GetOutput(args).Split(['\0'], StringSplitOptions.RemoveEmptyEntries);
            foreach (string objData in objects)
            {
                // "100644 blob b17a497cdc6140aa3b9a681344522f44768165ac 2120195\tBin/Dictionaries/de-DE.dic"
                string[] dataPack = objData.Split('\t');
                string[] data = dataPack[0].Split([' '], count: 4, StringSplitOptions.RemoveEmptyEntries);
                if (data[1] == "blob" && int.TryParse(data[3], out int size) && size >= thresholdSize)
                {
                    yield return new GitObject(data[2], dataPack[1], size, rev);
                }
            }
        }
    }
}
