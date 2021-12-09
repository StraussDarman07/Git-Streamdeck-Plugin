using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace Plugin.Models
{
    public class PluginRepository
    {
        private string RepositoryPath { get; }

        public string Head { get; private set; } = string.Empty;

        public IEnumerable<string> LocalBranches { get; private set; } = new List<string>();

        public IEnumerable<string> FavoriteBranches { get; private set; } = new List<string>();

        public bool Initialized { get; private set; }

        public PluginRepository(string path)
        {
            RepositoryPath = path;
        }

        public void Init()
        {
            using var repo = new Repository(RepositoryPath);

            UpdateRepository(repo);

            Initialized = true;
        }

        private void UpdateRepository(Repository repo)
        {
            Head = repo.Head.FriendlyName;

            LocalBranches = LoadLocalBranches(repo);

            FavoriteBranches = RepositoryUsageData.GetSortedBranch(repo.Info.WorkingDirectory);
        }

        private IEnumerable<string> LoadLocalBranches(Repository repository)
            => repository.Branches.Where(b => !b.IsRemote).Select(b => b.FriendlyName).ToList();

        public void CheckoutBranch(string branchName)
        {
            using var repo = new Repository(RepositoryPath);

            Branch branch = repo.Branches.FirstOrDefault(b => b.FriendlyName.Equals(branchName, StringComparison.OrdinalIgnoreCase)) ??
                throw new InvalidOperationException();

            Commands.Checkout(repo, branch);

            UpdateRepository(repo);

            RepositoryUsageData.UpdateBranch(repo.Info.WorkingDirectory, branchName);
        }
    }
}
