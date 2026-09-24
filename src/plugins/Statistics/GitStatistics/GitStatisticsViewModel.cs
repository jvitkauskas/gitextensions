using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.GitStatistics;

/// <summary>Strings of the Avalonia port of <see cref="FormGitStatistics"/>; ids match the form.</summary>
public sealed class GitStatisticsStrings : ViewStrings
{
    public GitStatisticsStrings()
        : base("FormGitStatistics")
    {
        Title = Add("$this", "Text", "Statistics");
        CommitsPerContributorTab = Add("tabPage2", "Text", "Commits per contributor");
        LinesOfCodePerLanguageTab = Add("tabPage1", "Text", "Lines of code per language");
        LinesOfCodePerTypeTab = Add("tabPage3", "Text", "Lines of code per type");
        LinesOfTestCodeTab = Add("tabPage4", "Text", "Lines of test code");
        TotalCommits = Add("TotalCommits", "Text", "Total commits");
        TotalLinesOfCode = Add("TotalLinesOfCode", "Text", "Total lines of code");
        TotalLinesOfCode2 = Add("TotalLinesOfCode2", "Text", "Total lines of code");
        TotalLinesOfTestCode = Add("TotalLinesOfTestCode", "Text", "Total lines of code");
        CommitStatisticsLoading = Add("CommitStatistics", "Text", "Loading..");
        LinesOfCodePerLanguageLoading = Add("LinesOfCodePerLanguageText", "Text", "Loading...");
        LinesOfCodePerTypeLoading = Add("LinesOfCodePerTypeText", "Text", "Loading...");
        TestCodeLoading = Add("TestCodeText", "Text", "Loading...");
        Commits = Add("_commits", "Text", "{0:N0} Commits");
        CommitsBy = Add("_commitsBy", "Text", "{0:N0} Commits by {1}");
        LinesOfCodeInFiles = Add("_linesOfCodeInFiles", "Text", "{0:N0} Lines of code in {1} files ({2:P1})");
        LinesOfCode = Add("_linesOfCode", "Text", "{0:N0} Lines of code");
        LinesOfCodeP = Add("_linesOfCodeP", "Text", "{0:N0} Lines of code ({1:P1})");
        LinesOfTestCode = Add("_linesOfTestCode", "Text", "{0:N0} Lines of test code");
        LinesOfTestCodeP = Add("_linesOfTestCodeP", "Text", "{0:N0} Lines of test code ({1:P1})");
        LinesOfProductionCodeP = Add("_linesOfProductionCodeP", "Text", "{0:N0} Lines of production code ({1:P1})");
        BlankLinesP = Add("_blankLinesP", "Text", "{0:N0} Blank lines ({1:P1})");
        CommentLinesP = Add("_commentLinesP", "Text", "{0:N0} Comment lines ({1:P1})");
        LinesOfDesignerFilesP = Add("_linesOfDesignerFilesP", "Text", "{0:N0} Lines in designer files ({1:P1})");
    }

    public TranslatedText Title { get; }

    public TranslatedText CommitsPerContributorTab { get; }

    public TranslatedText LinesOfCodePerLanguageTab { get; }

    public TranslatedText LinesOfCodePerTypeTab { get; }

    public TranslatedText LinesOfTestCodeTab { get; }

    public TranslatedText TotalCommits { get; }

    public TranslatedText TotalLinesOfCode { get; }

    public TranslatedText TotalLinesOfCode2 { get; }

    public TranslatedText TotalLinesOfTestCode { get; }

    public TranslatedText CommitStatisticsLoading { get; }

    public TranslatedText LinesOfCodePerLanguageLoading { get; }

    public TranslatedText LinesOfCodePerTypeLoading { get; }

    public TranslatedText TestCodeLoading { get; }

    public TranslatedText Commits { get; }

    public TranslatedText CommitsBy { get; }

    public TranslatedText LinesOfCodeInFiles { get; }

    public TranslatedText LinesOfCode { get; }

    public TranslatedText LinesOfCodeP { get; }

    public TranslatedText LinesOfTestCode { get; }

    public TranslatedText LinesOfTestCodeP { get; }

    public TranslatedText LinesOfProductionCodeP { get; }

    public TranslatedText BlankLinesP { get; }

    public TranslatedText CommentLinesP { get; }

    public TranslatedText LinesOfDesignerFilesP { get; }
}

/// <summary>A slice of a pie chart: its value and its tooltip (as <c>PieChartControl.SetValues</c> and <c>ToolTips</c>).</summary>
public sealed record PieSlice(decimal Value, string ToolTip);

/// <summary>The counts of a <see cref="LineCounter"/> at a point of the counting.</summary>
public sealed record LinesOfCodeCounts(int Code, int Test, int Blank, int Comment, int Designer, int Total, IReadOnlyList<KeyValuePair<string, int>> PerExtension);

/// <summary>View model of the Avalonia port of <see cref="FormGitStatistics"/>.</summary>
public sealed partial class GitStatisticsViewModel : DialogViewModel
{
    private readonly IGitModule _module;
    private readonly Func<string, IGitModule> _openSubmodule;
    private readonly string _codeFilePattern;
    private readonly bool _countSubmodules;
    private readonly string _directoriesToIgnore;
    private readonly IBackgroundRunner _backgroundRunner;
    private LineCounter? _lineCounter;

    /// <param name="openSubmodule">Opens the submodule at the given full path (a <c>GitModule</c>).</param>
    /// <param name="directoriesToIgnore">The directories not counted (<c>DirectoriesToIgnore</c>).</param>
    public GitStatisticsViewModel(
        GitStatisticsStrings strings,
        IGitModule module,
        Func<string, IGitModule> openSubmodule,
        string codeFilePattern,
        bool countSubmodules,
        string directoriesToIgnore,
        IBackgroundRunner backgroundRunner)
    {
        Strings = strings;
        _module = module;
        _openSubmodule = openSubmodule;
        _codeFilePattern = codeFilePattern;
        _countSubmodules = countSubmodules;
        _directoriesToIgnore = directoriesToIgnore;
        _backgroundRunner = backgroundRunner;

        TotalCommitsText = strings.TotalCommits.Text;
        CommitStatisticsText = strings.CommitStatisticsLoading.Text;
        TotalLinesOfCodeText = strings.TotalLinesOfCode.Text;
        LinesOfCodePerLanguageText = strings.LinesOfCodePerLanguageLoading.Text;
        TotalLinesOfCode2Text = strings.TotalLinesOfCode2.Text;
        LinesOfCodePerTypeText = strings.LinesOfCodePerTypeLoading.Text;
        TotalLinesOfTestCodeText = strings.TotalLinesOfTestCode.Text;
        TestCodeText = strings.TestCodeLoading.Text;
    }

    public GitStatisticsStrings Strings { get; }

    [ObservableProperty]
    public partial string TotalCommitsText { get; private set; }

    [ObservableProperty]
    public partial string CommitStatisticsText { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<PieSlice> CommitSlices { get; private set; } = [];

    [ObservableProperty]
    public partial string TotalLinesOfCodeText { get; private set; }

    [ObservableProperty]
    public partial string LinesOfCodePerLanguageText { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<PieSlice> ExtensionSlices { get; private set; } = [];

    [ObservableProperty]
    public partial string TotalLinesOfCode2Text { get; private set; }

    [ObservableProperty]
    public partial string LinesOfCodePerTypeText { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<PieSlice> TypeSlices { get; private set; } = [];

    [ObservableProperty]
    public partial string TotalLinesOfTestCodeText { get; private set; }

    [ObservableProperty]
    public partial string TestCodeText { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<PieSlice> TestSlices { get; private set; } = [];

    /// <summary>As <c>FormGitStatisticsShown</c> (<c>Initialize</c>): counts the commits and the lines of code in the background.</summary>
    public Task LoadAsync() => Task.WhenAll(LoadCommitCountAsync(), LoadLinesOfCodeAsync());

    /// <summary>As <c>InitializeCommitCount</c>.</summary>
    private async Task LoadCommitCountAsync()
    {
        (int totalCommits, Dictionary<string, int> commitsPerUser) = await _backgroundRunner.RunAsync(() => _module.GetCommitsByContributor());

        TotalCommitsText = string.Format(Strings.Commits.Text, totalCommits);

        StringBuilder builder = new();
        List<PieSlice> slices = new(commitsPerUser.Count);
        foreach ((string user, int commits) in commitsPerUser)
        {
            builder.AppendLine($"{commits:N0} {user}");
            slices.Add(new PieSlice(commits, string.Format(Strings.CommitsBy.Text, commits, user)));
        }

        CommitSlices = slices;
        CommitStatisticsText = builder.ToString();
    }

    /// <summary>As <c>InitializeLinesOfCode</c>: the counts are shown while the files are analyzed (<c>LineCounter.Updated</c>).</summary>
    private async Task LoadLinesOfCodeAsync()
    {
        if (_lineCounter is not null)
        {
            return;
        }

        LineCounter lineCounter = new();
        _lineCounter = lineCounter;
        lineCounter.Updated += OnLineCounterUpdated;
        try
        {
            await _backgroundRunner.RunAsync(() =>
            {
                LoadLinesOfCodeForModule(lineCounter, _module);

                if (_countSubmodules)
                {
                    foreach (IGitSubmoduleInfo? submodule in _module.GetSubmodulesInfo())
                    {
                        if (submodule is not null)
                        {
                            LoadLinesOfCodeForModule(lineCounter, _openSubmodule(Path.Combine(_module.WorkingDir, submodule.LocalPath)));
                        }
                    }
                }

                // As the WinForms form: the final counts.
                OnLineCounterUpdated(lineCounter, EventArgs.Empty);
                return true;
            });
        }
        finally
        {
            lineCounter.Updated -= OnLineCounterUpdated;
        }
    }

    private void LoadLinesOfCodeForModule(LineCounter lineCounter, IGitModule module)
    {
        List<string> filesToCheck = [.. module
            .GetTree(commitId: default, full: true)
            .Select(file => Path.Combine(module.WorkingDir, file.Name))];

        lineCounter.FindAndAnalyzeCodeFiles(_codeFilePattern, _directoriesToIgnore, filesToCheck);
    }

    /// <summary>As <c>OnLineCounterUpdated</c>: reads the counts on the counting thread, then shows them on the UI thread.</summary>
    private void OnLineCounterUpdated(object? sender, EventArgs e)
    {
        LineCounter lineCounter = (LineCounter)sender!;
        LinesOfCodeCounts counts = new(
            lineCounter.CodeLineCount,
            lineCounter.TestCodeLineCount,
            lineCounter.BlankLineCount,
            lineCounter.CommentLineCount,
            lineCounter.DesignerLineCount,
            lineCounter.TotalLineCount,
            [.. lineCounter.LinesOfCodePerExtension]);
        _backgroundRunner.Post(() => ShowLinesOfCode(counts));
    }

    /// <summary>As <c>OnLineCounterUpdated</c> and <c>UpdateUI</c>: shows the counts (while counting, and at the end).</summary>
    public void ShowLinesOfCode(LinesOfCodeCounts counts)
    {
        List<KeyValuePair<string, int>> linesOfCodePerExtension = [.. counts.PerExtension];
        linesOfCodePerExtension.Sort((first, next) => -first.Value.CompareTo(next.Value));

        StringBuilder linesOfCodePerLanguageText = new();
        List<PieSlice> extensionSlices = new(linesOfCodePerExtension.Count);
        foreach ((string extension, int loc) in linesOfCodePerExtension)
        {
            double percent = (double)loc / counts.Code;
            string line = string.Format(Strings.LinesOfCodeInFiles.Text, loc, extension, percent);
            linesOfCodePerLanguageText.AppendLine(line);
            extensionSlices.Add(new PieSlice(loc, line));
        }

        TotalLinesOfTestCodeText = string.Format(Strings.LinesOfTestCode.Text, counts.Test);

        int production = counts.Code - counts.Test;
        double percentTest = (double)counts.Test / counts.Code;
        double percentProd = (double)production / counts.Code;
        string testLine = string.Format(Strings.LinesOfTestCodeP.Text, counts.Test, percentTest);
        string productionLine = string.Format(Strings.LinesOfProductionCodeP.Text, production, percentProd);
        TestSlices = [new PieSlice(counts.Test, testLine), new PieSlice(production, productionLine)];
        TestCodeText = testLine + Environment.NewLine + productionLine;

        double percentBlank = (double)counts.Blank / counts.Total;
        double percentComments = (double)counts.Comment / counts.Total;
        double percentCode = (double)counts.Code / counts.Total;
        double percentDesigner = (double)counts.Designer / counts.Total;
        TypeSlices =
        [
            new PieSlice(counts.Blank, string.Format(Strings.BlankLinesP.Text, counts.Blank, percentBlank)),
            new PieSlice(counts.Comment, string.Format(Strings.CommentLinesP.Text, counts.Comment, percentComments)),
            new PieSlice(counts.Code, string.Format(Strings.LinesOfCodeP.Text, counts.Code, percentCode)),
            new PieSlice(counts.Designer, string.Format(Strings.LinesOfDesignerFilesP.Text, counts.Designer, percentDesigner)),
        ];
        LinesOfCodePerTypeText = string.Join(Environment.NewLine, TypeSlices.Select(slice => slice.ToolTip));

        LinesOfCodePerLanguageText = linesOfCodePerLanguageText.ToString();
        ExtensionSlices = extensionSlices;

        TotalLinesOfCode2Text = TotalLinesOfCodeText = string.Format(Strings.LinesOfCode.Text, counts.Code);
    }
}
