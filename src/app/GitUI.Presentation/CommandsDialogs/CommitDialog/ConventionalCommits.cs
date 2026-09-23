namespace GitUI.Presentation.CommandsDialogs.CommitDialog;

/// <summary>The Conventional Commits items of the templates menu (port of <c>FormCommit.AddConventionalCommitsItems</c>).</summary>
public static class ConventionalCommits
{
    public const string Feat = "feat";

    /// <summary>The documentation (<c>_conventionalCommitDocumentation</c>).</summary>
    public const string DocumentationUrl = "https://www.conventionalcommits.org";

    /// <summary>The types of the subject (<c>_headerCommitTypes</c>).</summary>
    public static IReadOnlyList<string> HeaderTypes { get; } = ["build", "chore", "ci", "docs", Feat, "fix", "perf", "refactor", "style", "test"];

    /// <summary>The keywords of the footer (<c>_footerKeywords</c>), then <c>[skip ci]</c>.</summary>
    public static IReadOnlyList<string> FooterKeywords { get; } = ["BREAKING CHANGE", "Co-authored-by", "Reviewed-by"];

    public const string SkipCi = "[skip ci]";

    /// <summary>
    ///  As <c>PrefixOrReplaceKeyword</c>: the first line with <paramref name="keyword"/> as its type (replacing a type), and
    ///  where the caret goes.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="caret">The offset of the caret in the message.</param>
    public static (string Title, int SelectionStart) PrefixOrReplaceKeyword(string message, int caret, string keyword, bool insertScope)
    {
        string scope = insertScope ? "()" : "";
        int scopePosition = keyword.Length + 1;
        int titlePosition = keyword.Length + (scope.Length / 2) + 2;
        string currentTitle = string.IsNullOrWhiteSpace(message) ? string.Empty : FirstLine(message);

        // Replacing current keyword
        foreach (string key in HeaderTypes)
        {
            if (!currentTitle.StartsWith(key))
            {
                continue;
            }

            if (currentTitle.Length == key.Length)
            {
                return ($"{keyword}{scope}: ", insertScope ? scopePosition : titlePosition);
            }

            char nextChar = currentTitle[key.Length];
            if (!insertScope)
            {
                if (nextChar == ':' || nextChar == '(' || nextChar == '!')
                {
                    return ReplaceKeyword(_ => titlePosition);
                }
            }
            else
            {
                if (nextChar == ':' || nextChar == '!')
                {
                    return ($"{keyword}(){currentTitle[key.Length..]}", scopePosition);
                }

                if (nextChar == '(')
                {
                    return ReplaceKeyword(newTitle => 2 + Math.Max(newTitle.IndexOf(':'), newTitle.IndexOf('(')));
                }
            }

            (string Title, int SelectionStart) ReplaceKeyword(Func<string, int> maxPosition)
            {
                string newTitle = $"{keyword}{currentTitle[key.Length..]}";
                int newMessageLength = message.Length + newTitle.Length - currentTitle.Length;
                return (newTitle, Math.Min(newMessageLength, Math.Max(maxPosition(newTitle), caret + keyword.Length - key.Length)));
            }
        }

        // Append current keyword
        return ($"{keyword}{scope}: {currentTitle}", insertScope ? scopePosition : titlePosition + caret);
    }

    /// <summary>The message with <paramref name="title"/> as its first line (as <c>ReplaceLine(0, title)</c>).</summary>
    public static string ReplaceFirstLine(string message, string title)
    {
        if (message.Length == 0)
        {
            return title;
        }

        int end = message.IndexOf('\n');
        if (end < 0)
        {
            return title;
        }

        if (end > 0 && message[end - 1] == '\r')
        {
            end--;
        }

        return title + message[end..];
    }

    /// <summary>As <c>AddFooter</c>: the text on a new line after the last one.</summary>
    public static string AddFooter(string message, string footer)
        => message.Length == 0 ? $"{Environment.NewLine}{footer}" : $"{message}{Environment.NewLine}{footer}";

    private static string FirstLine(string text)
    {
        int end = text.IndexOfAny(['\r', '\n']);
        return end < 0 ? text : text[..end];
    }
}
