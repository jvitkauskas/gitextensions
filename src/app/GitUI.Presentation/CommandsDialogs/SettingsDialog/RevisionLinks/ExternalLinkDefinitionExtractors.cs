using GitCommands.ExternalLinks;
using GitCommands.Remotes;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.RevisionLinks;

/// <summary>Port of <c>CloudProviderKind</c>: the providers with templates of revision links.</summary>
public enum CloudProviderKind
{
    None = 0,
    GitHub,
    AzureDevOps,
}

/// <summary>
///  Port of <c>ICloudProviderExternalLinkDefinitionExtractor</c>: the templates of the revision links of a provider, for the
///  repository of a remote.
/// </summary>
public interface ICloudProviderExternalLinkDefinitionExtractor
{
    string ServiceName { get; }

    /// <summary>The name of the icon of the provider (an asset of GitUI.Avalonia).</summary>
    string IconName { get; }

    bool IsValidRemoteUrl(string remoteUrl);

    IList<ExternalLinkDefinition> GetDefinitions(string remoteUrl);
}

/// <summary>Port of <c>ICloudProviderExternalLinkDefinitionExtractorFactory</c>.</summary>
public interface ICloudProviderExternalLinkDefinitionExtractorFactory
{
    ICloudProviderExternalLinkDefinitionExtractor? Get(CloudProviderKind cloudProviderKind);
}

/// <summary>Port of <c>CloudProviderExternalLinkDefinitionExtractorFactory</c>.</summary>
public sealed class CloudProviderExternalLinkDefinitionExtractorFactory : ICloudProviderExternalLinkDefinitionExtractorFactory
{
    public ICloudProviderExternalLinkDefinitionExtractor? Get(CloudProviderKind cloudProviderKind)
    {
        return cloudProviderKind switch
        {
            CloudProviderKind.GitHub => new GitHubExternalLinkDefinitionExtractor(),
            CloudProviderKind.AzureDevOps => new AzureDevopsExternalLinkDefinitionExtractor(),
            _ => null
        };
    }

    public IEnumerable<ICloudProviderExternalLinkDefinitionExtractor> GetAllExtractor()
        => Enum.GetValues<CloudProviderKind>().Select(Get).WhereNotNull();
}

/// <summary>Port of <c>ExternalLinkDefinitionExtractor</c>.</summary>
/// <remarks>
///  The names of the templates are not translated: the <c>TranslationString</c>s of the WinForms class are not in the
///  translations (the class is no <c>Translate</c>).
/// </remarks>
public abstract class ExternalLinkDefinitionExtractor : ICloudProviderExternalLinkDefinitionExtractor
{
    private protected const string CodeLink = "{0} - Code";
    private protected const string IssuesLink = "{0} - Issues";
    private protected const string PullRequestsLink = "{0} - Pull Requests";
    private protected const string ViewCommitLink = "View commit in {0}";
    private protected const string ViewProjectLink = "View project in {0}";

    public abstract string ServiceName { get; }

    public abstract string IconName { get; }

    public abstract bool IsValidRemoteUrl(string remoteUrl);

    public abstract IList<ExternalLinkDefinition> GetDefinitions(string remoteUrl);
}

/// <summary>Port of <c>GitHubExternalLinkDefinitionExtractor</c>.</summary>
public sealed class GitHubExternalLinkDefinitionExtractor : ExternalLinkDefinitionExtractor
{
    private readonly GitHubRemoteParser _remoteParser = new();

    public override string ServiceName => "GitHub";

    public override string IconName => "GitHub";

    public override bool IsValidRemoteUrl(string remoteUrl) => _remoteParser.IsValidRemoteUrl(remoteUrl);

    public override IList<ExternalLinkDefinition> GetDefinitions(string remoteUrl)
    {
        List<ExternalLinkDefinition> externalLinkDefinitions = [];
        string? organizationName = null;
        string? repoName = null;

        if (!string.IsNullOrWhiteSpace(remoteUrl))
        {
            _remoteParser.TryExtractGitHubDataFromRemoteUrl(remoteUrl, out organizationName, out repoName);
        }

        organizationName ??= "ORGANIZATION_NAME";
        repoName ??= "REPO_NAME";

        string gitHubUrl = $"https://github.com/{organizationName}/{repoName}";
        ExternalLinkDefinition definition = new()
        {
            Name = string.Format(CodeLink, ServiceName),
            Enabled = true,
            SearchInParts = { ExternalLinkDefinition.RevisionPart.Message },
            SearchPattern = ".*",
            LinkFormats =
            {
                new ExternalLinkFormat { Caption = string.Format(ViewCommitLink, ServiceName), Format = gitHubUrl + "/commit/%COMMIT_HASH%" },
                new ExternalLinkFormat { Caption = string.Format(ViewProjectLink, ServiceName), Format = gitHubUrl }
            }
        };
        externalLinkDefinitions.Add(definition);

        externalLinkDefinitions.Add(new ExternalLinkDefinition
        {
            Name = string.Format(IssuesLink, ServiceName),
            Enabled = true,
            SearchInParts = { ExternalLinkDefinition.RevisionPart.Message, ExternalLinkDefinition.RevisionPart.LocalBranches },
            SearchPattern = @"(?i)(?<!pull request |pr[ _]?)(#|(((feat(ure)?)|fix)[/_-]))\d+",
            NestedSearchPattern = @"\d+",
            LinkFormats = { new ExternalLinkFormat { Caption = "#{0}", Format = gitHubUrl + "/issues/{0}" } }
        });

        externalLinkDefinitions.Add(new ExternalLinkDefinition
        {
            Name = string.Format(PullRequestsLink, ServiceName),
            Enabled = true,
            SearchInParts = { ExternalLinkDefinition.RevisionPart.Message, ExternalLinkDefinition.RevisionPart.LocalBranches, ExternalLinkDefinition.RevisionPart.RemoteBranches },
            SearchPattern = @"(?i)(pull request |pr[ _]?)#?\d+",
            NestedSearchPattern = @"\d+",
            LinkFormats = { new ExternalLinkFormat { Caption = "PR #{0}", Format = gitHubUrl + "/pull/{0}" } }
        });

        return externalLinkDefinitions;
    }
}

/// <summary>Port of <c>AzureDevopsExternalLinkDefinitionExtractor</c>.</summary>
public sealed class AzureDevopsExternalLinkDefinitionExtractor : ExternalLinkDefinitionExtractor
{
    private readonly AzureDevOpsRemoteParser _azureDevOpsRemoteParser = new();

    public override string ServiceName => "Azure DevOps";

    public override string IconName => "VisualStudioTeamServices";

    public override bool IsValidRemoteUrl(string remoteUrl) => _azureDevOpsRemoteParser.IsValidRemoteUrl(remoteUrl);

    public override IList<ExternalLinkDefinition> GetDefinitions(string remoteUrl)
    {
        List<ExternalLinkDefinition> externalLinkDefinitions = [];
        string? accountName = null;
        string? repoName = null;

        if (!string.IsNullOrWhiteSpace(remoteUrl))
        {
            _azureDevOpsRemoteParser.TryExtractAzureDevopsDataFromRemoteUrl(remoteUrl, out accountName, out _, out repoName);
        }

        accountName ??= "ACCOUNT_NAME";
        repoName ??= "REPO_NAME";

        string azureDevopsUrl = $"https://dev.azure.com/{accountName}";
        ExternalLinkDefinition definition = new()
        {
            Name = string.Format(CodeLink, ServiceName),
            Enabled = true,
            SearchInParts = { ExternalLinkDefinition.RevisionPart.Message },
            SearchPattern = @".*",
            LinkFormats =
            {
                new ExternalLinkFormat { Caption = string.Format(ViewCommitLink, ServiceName), Format = $"{azureDevopsUrl}/_git/{repoName}/commit/%COMMIT_HASH%" },
                new ExternalLinkFormat { Caption = string.Format(ViewProjectLink, ServiceName), Format = $"{azureDevopsUrl}/{repoName}" }
            }
        };
        externalLinkDefinitions.Add(definition);

        externalLinkDefinitions.Add(new ExternalLinkDefinition
        {
            Name = string.Format(IssuesLink, ServiceName),
            Enabled = true,
            SearchInParts = { ExternalLinkDefinition.RevisionPart.Message },
            SearchPattern = @"#(\d+)",
            LinkFormats = { new ExternalLinkFormat { Caption = "#{0}", Format = $"{azureDevopsUrl}/{repoName}/_workitems/edit/{{0}}" } }
        });

        return externalLinkDefinitions;
    }
}
