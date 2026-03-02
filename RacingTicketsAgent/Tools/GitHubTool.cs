using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Configuration;
using Octokit;
using Repository = LibGit2Sharp.Repository;
using Signature = LibGit2Sharp.Signature;
using Credentials = LibGit2Sharp.Credentials;

namespace RacingTicketsAgent.Tools;

public class GitTool
{
    private readonly string _token;
    private readonly string _username;
    private readonly string _repoName;
    private readonly string _repoDescription;
    private readonly string _localPath;

    public GitTool(IConfiguration config, string localPath)
    {
        _token = config["GitHub:Token"] ?? throw new Exception("GitHub:Token missing");
        _username = config["GitHub:Username"] ?? throw new Exception("GitHub:Username missing");
        _repoName = config["GitHub:RepoName"] ?? "racing-tickets-frontend";
        _repoDescription = config["GitHub:RepoDescription"] ?? "";
        _localPath = localPath;
    }

    public async Task<string> EnsureRepoExistsAsync(CancellationToken ct = default)
    {
        var github = new GitHubClient(new ProductHeaderValue("RacingTicketsAgent"))
        {
            Credentials = new Octokit.Credentials(_token)
        };

        try
        {
            var existing = await github.Repository.Get(_username, _repoName);
            return existing.CloneUrl;
        }
        catch (Octokit.NotFoundException)
        {
            var created = await github.Repository.Create(new NewRepository(_repoName)
            {
                Description = _repoDescription,
                Private = false,
                AutoInit = false
            });
            return created.CloneUrl;
        }
    }

    public async Task<string> CommitAndPushAsync(string message, CancellationToken ct = default)
    {
        try
        {
            var remoteUrl = await EnsureRepoExistsAsync(ct);

            // Init if needed
            if (!Repository.IsValid(_localPath))
                Repository.Init(_localPath);

            using var repo = new Repository(_localPath);

            // Write .gitignore if missing
            EnsureGitIgnore();

            // Set remote
            if (repo.Network.Remotes["origin"] == null)
                repo.Network.Remotes.Add("origin", remoteUrl);
            else
                repo.Network.Remotes.Update("origin", r => r.Url = remoteUrl);

            // Stage all
            Commands.Stage(repo, "*");

            var status = repo.RetrieveStatus();
            if (!status.IsDirty)
                return "Nothing to commit — no changes detected.";

            // Commit
            var sig = new Signature(_username, $"{_username}@racing-agent.local", DateTimeOffset.Now);
            repo.Commit(message, sig, sig);

            // Push
            var options = new PushOptions
            {
                CredentialsProvider = (_, _, _) =>
                    new UsernamePasswordCredentials { Username = _token, Password = string.Empty }
            };
            repo.Network.Push(repo.Head, options);

            return $"✓ Committed and pushed: \"{message}\"";
        }
        catch (Exception ex)
        {
            return $"Git error: {ex.Message}";
        }
    }

    private void EnsureGitIgnore()
    {
        var path = Path.Combine(_localPath, ".gitignore");
        if (File.Exists(path)) return;

        File.WriteAllText(path, """
            # Secrets
            appsettings.json
            appsettings.*.json

            # Angular
            node_modules/
            dist/
            .angular/

            # Build
            bin/
            obj/

            # OS
            .DS_Store
            Thumbs.db
            """);
    }
}