using System.Diagnostics;
using System.Net.Mail;
using System.Text.RegularExpressions;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.UserControls;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The patch grid of the Avalonia rebase and apply patch dialogs: reads the rebase directory as the WinForms <c>PatchGrid</c>
///  does (<c>GetInteractiveRebasePatchFiles</c>, <c>GetRebasePatchFiles</c>, duplicated here; re-port upstream changes of
///  <c>PatchGrid.cs</c>, which <c>PatchGridPortParityTests</c> compares).
/// </summary>
internal sealed partial class PatchGridHost(IGitUICommands commands) : IPatchGridHost
{
    [GeneratedRegex(@"^(?<header_key>[-A-Za-z0-9]+)(?::[ \t]*)(?<header_value>.*)$", RegexOptions.ExplicitCapture)]
    private static partial Regex HeadersRegex { get; }
    [GeneratedRegex(@"=\?(?<qr1>[\w-]+)\?q\?(<qr2>.*)\?=$", RegexOptions.ExplicitCapture)]
    private static partial Regex QuotedRegex { get; }

    private IGitModule Module => commands.Module;

    /// <summary>As <c>PatchGrid.GetPatches</c>, before marking the skipped ones.</summary>
    public IReadOnlyList<PatchItem> LoadPatches()
    {
        string rebaseTodoFilePath = $"{Module.GetRebaseDir()}git-rebase-todo";
        return File.Exists(rebaseTodoFilePath)
            ? GetInteractiveRebasePatchFiles()
            : GetRebasePatchFiles();
    }

    public void ShowCommit(ObjectId objectId) => AvaloniaUi.RunInHostContext(() => commands.StartFormCommitDiff(objectId));

    public void ShowPatch(string fullName) => AvaloniaUi.RunInHostContext(() => commands.StartViewPatchDialog(fullName));

    /// <summary>As <c>PatchGrid.GetInteractiveRebasePatchFiles</c>.</summary>
    internal IReadOnlyList<PatchItem> GetInteractiveRebasePatchFiles()
    {
        string rebaseDir = Module.GetRebaseDir();
        string currentFilePath = $"{rebaseDir}stopped-sha";
        string doneFilePath = $"{rebaseDir}done";
        string rebaseTodoFilePath = $"{rebaseDir}git-rebase-todo";

        string[] doneCommits = ReadCommitsDataFromRebaseFile(doneFilePath);
        string[] todoCommits = ReadCommitsDataFromRebaseFile(rebaseTodoFilePath);
        string commentChar = Module.GetEffectiveSetting("core.commentchar", defaultValue: "#");

        // Filter comment lines and keep only lines containing at least 3 columns
        // (action, commit hash and commit subject -- that could contain spaces and be cut in more --)
        // ex: pick e0d861716540aa1ac83eaa2790ba5e79988b9489 this is the commit subject
        string[][] commitsInfos = [.. doneCommits.Concat(todoCommits).Where(l => !l.StartsWith(commentChar))
            .Select(l => l.Split(Delimiters.Space))
            .Where(p => p.Length >= 3)];

        List<PatchItem> patchFiles = [];
        if (commitsInfos.Length == 0)
        {
            return patchFiles;
        }

        CommitDataManager commitDataManager = new(() => Module);
        RevisionReader reader = new(Module, allBodies: true);
        Dictionary<string, GitRevision> rebasedCommitsRevisions;
        try
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            rebasedCommitsRevisions = reader.GetRevisionsFromRange(commitsInfos[0][1], commitsInfos[^1][1], cts.Token)
                .ToDictionary(r => r.Guid, r => r);
        }
        catch (OperationCanceledException)
        {
            // If retrieve of commit range failed, fall back on getting data commit by commit
            rebasedCommitsRevisions = [];
        }

        string? currentCommitShortHash = File.Exists(currentFilePath) ? File.ReadAllText(currentFilePath).Trim() : null;
        bool isCurrentFound = false;
        foreach (string[] parts in commitsInfos)
        {
            string commitHash = parts[1];
            CommitData? data = rebasedCommitsRevisions.TryGetValue(commitHash, out GitRevision? commitRevision)
                ? commitDataManager.CreateFromRevision(commitRevision, null)
                : commitDataManager.GetCommitData(commitHash);

            bool isApplying = currentCommitShortHash is not null && commitHash.StartsWith(currentCommitShortHash);
            isCurrentFound |= isApplying;

            ObjectId objectId;
            if (data is not null)
            {
                objectId = data.ObjectId;
            }
            else
            {
                if (!ObjectId.TryParse(parts[1], out objectId))
                {
                    Trace.Write($"PatchGrid: GetInteractiveRebasePatchFiles: Unable to parse commit hash '{parts[1]}' from '{todoCommits}'. Skipping this entry.");
                    continue;
                }
            }

            patchFiles.Add(new PatchItem
            {
                Action = parts[0],
                ObjectId = objectId,

                // During a rebase, "Patch" subject is filled with commit **body** to display it
                // packed in the grid and more readable in the cell tooltip
                Subject = data?.Body ?? string.Join(' ', parts.Skip(2)),
                Author = data?.Author,
                Date = data?.CommitDate.LocalDateTime.ToString(),
                IsNext = isApplying,
                IsApplied = !isCurrentFound,
            });
        }

        return patchFiles;

        static string[] ReadCommitsDataFromRebaseFile(string filePath) =>
            File.Exists(filePath)
            ? File.ReadAllText(filePath).Trim().Split(Delimiters.LineFeedAndCarriageReturn, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [];
    }

    /// <summary>As <c>PatchGrid.GetNextRebasePatch</c>.</summary>
    private string GetNextRebasePatch()
    {
        string file = $"{Module.GetRebaseDir()}next";
        return File.Exists(file) ? File.ReadAllText(file).Trim() : "";
    }

    /// <summary>As <c>PatchGrid.GetRebasePatchFiles</c>.</summary>
    internal IReadOnlyList<PatchItem> GetRebasePatchFiles()
    {
        List<PatchItem> patchFiles = [];

        string nextFile = GetNextRebasePatch();

        if (!int.TryParse(nextFile, out int next))
        {
            Trace.Write($"PatchGrid: GetRebasePatchFiles: Unable to parse rebase next patch file name '{nextFile}'. Skipping this file.");
            next = 0;
        }

        string rebaseDir = Module.GetRebaseDir();

        string[] files = Directory.Exists(rebaseDir)
            ? Directory.GetFiles(rebaseDir)
            : [];

        foreach (string fullFileName in files)
        {
            string file = PathUtil.GetFileName(fullFileName);
            if (!int.TryParse(file, out int n))
            {
                continue;
            }

            PatchItem patchFile =
                new()
                {
                    Name = file,
                    FullName = fullFileName,
                    IsApplied = n < next,
                    IsNext = n == next
                };

            if (File.Exists(rebaseDir + file))
            {
                string? key = null;
                string value = "";
                foreach (string line in File.ReadLines(rebaseDir + file))
                {
                    Match m = HeadersRegex.Match(line);
                    if (key is null)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !m.Success)
                        {
                            continue;
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(line) || m.Success)
                    {
                        // decode QuotedPrintable text using .NET internal decoder
                        value = Attachment.CreateAttachmentFromString("", value).Name!;
                        switch (key)
                        {
                            case "From":
                                if (value.IndexOf('<') > 0 && value.IndexOf('<') < value.Length)
                                {
                                    string author = RFC2047Decoder.Parse(value);
                                    patchFile.Author = author[..author.IndexOf('<')].Trim();
                                }
                                else
                                {
                                    patchFile.Author = value;
                                }

                                break;
                            case "Date":
                                if (value.IndexOf('+') > 0 && value.IndexOf('<') < value.Length)
                                {
                                    patchFile.Date = value[..value.IndexOf('+')].Trim();
                                }
                                else
                                {
                                    patchFile.Date = value;
                                }

                                break;
                            case "Subject":
                                patchFile.Subject = value;
                                break;
                        }
                    }

                    if (m.Success)
                    {
                        key = m.Groups["header_key"].Value;
                        value = m.Groups["header_value"].Value;
                    }
                    else if (!string.IsNullOrEmpty(line))
                    {
                        value = AppendQuotedString(value, line.Trim())!;
                    }

                    if (string.IsNullOrEmpty(line) ||
                        (!string.IsNullOrEmpty(patchFile.Author) &&
                        !string.IsNullOrEmpty(patchFile.Date) &&
                        !string.IsNullOrEmpty(patchFile.Subject)))
                    {
                        break;
                    }
                }
            }

            patchFiles.Add(patchFile);
        }

        return patchFiles;

        static string AppendQuotedString(string str1, string str2)
        {
            Match m1 = QuotedRegex.Match(str1);
            Match m2 = QuotedRegex.Match(str2);
            if (!m1.Success || !m2.Success)
            {
                return str1 + str2;
            }

            DebugHelpers.Assert(m1.Groups["qr1"].Value == m2.Groups["qr1"].Value, @"m1.Groups[""qr1""].Value == m2.Groups[""qr2""].Value");
            return $"{str1.AsSpan()[..^2]}{m2.Groups["qr2"].ValueSpan}?=";
        }
    }
}
