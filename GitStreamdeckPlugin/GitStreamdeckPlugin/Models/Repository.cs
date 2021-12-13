using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Plugin.Misc;

namespace Plugin.Models
{
    public class PluginRepository
    {
        private string RepositoryPath { get; }

        public string Head { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public IEnumerable<PluginBranch> LocalBranches { get; private set; } = new List<PluginBranch>();
        public IEnumerable<PluginBranch> RemoteBranches { get; private set; } = new List<PluginBranch>();
        public IEnumerable<PluginBranch> FavoriteBranches { get; private set; } = new List<PluginBranch>();

        public IEnumerable<PluginBranch> MergedBranches { get; private set; } = new List<PluginBranch>();

        public bool Initialized { get; private set; }

        public PluginRepository(string path)
        {
            RepositoryPath = path;
        }

        public void Init()
        {
            using var repo = new Repository(RepositoryPath);

            FetchRepository(repo);

            UpdateRepository(repo);

            Initialized = true;
        }

        private void FetchRepository(Repository repo)
        {
            var remote = repo.Network.Remotes["origin"];
            FetchOptions options = new FetchOptions {TagFetchMode = TagFetchMode.All};
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
            Commands.Fetch(repo, remote.Name, refSpecs, null, "Fetching remote");
            
        }

        private void UpdateRepository(Repository repo)
        {
            Head = repo.Head.FriendlyName;

            Name = repo.Info.WorkingDirectory;

            LocalBranches = LoadLocalBranches(repo);

            RemoteBranches = LoadRemoteBranches(repo);

            FavoriteBranches = LoadFavoriteBranches(repo);

            MergedBranches = LoadMergedBranches(repo);
        }

        private static IEnumerable<PluginBranch> LoadLocalBranches(Repository repository)
            => repository.Branches.Where(b => !b.IsRemote).Select(b => new PluginBranch(b)).ToList();

        private static IEnumerable<PluginBranch> LoadRemoteBranches(Repository repository)
            => repository.Branches.Where(b => b.IsRemote).Select(b => new PluginBranch(b)).ToList();

        private static IEnumerable<PluginBranch> LoadFavoriteBranches(Repository repository)
        {
            IEnumerable<string> favoriteBranches = RepositoryUsageData.GetSortedBranch(repository.Info.WorkingDirectory);
            IEnumerable<PluginBranch> localBranches = LoadLocalBranches(repository);

            //TODO prune invalid branches
            return favoriteBranches.Where(fav => localBranches.Any(local => local.DisplayName.Equals(fav, StringComparison.Ordinal)))
                .Select(fav => localBranches.First(local => local.DisplayName.Equals(fav, StringComparison.Ordinal)));
        }

        private static IEnumerable<PluginBranch> LoadMergedBranches(Repository repository)
        {
            IEnumerable<PluginBranch> localBranches = LoadLocalBranches(repository);
            IEnumerable<PluginBranch> remoteBranches = LoadRemoteBranches(repository);

            Debug.Assert(localBranches != null);
            Debug.Assert(remoteBranches != null);

            IEnumerable<PluginBranch> localTrackingBranches = localBranches.Where(local => local.Branch.IsTracking);
            IEnumerable<PluginBranch> filteredRemotes = remoteBranches.Where(remote => !localTrackingBranches.Any(local => local.Branch.TrackedBranch.Equals(remote.Branch)));
            return filteredRemotes.Concat(localBranches).ToList();
        }

        public void CheckoutBranch(PluginBranch checkoutBranch)
        {
            using var repo = new Repository(RepositoryPath);

            Branch branch = repo.Branches.FirstOrDefault(b => b.Equals(checkoutBranch.Branch)) ??
                throw new InvalidOperationException();

            if (branch.IsRemote)
                HandleRemoteBranchCheckout(repo, branch);

            UpdateRepository(repo);

            RepositoryUsageData.UpdateBranch(repo.Info.WorkingDirectory, checkoutBranch.DisplayName);
        }

        private void HandleRemoteBranchCheckout(Repository repo, Branch remoteBranch)
        {
            Branch? trackingBranch = GetLocalTrackingBranch(repo, remoteBranch);
            if (trackingBranch != null)
            {
                Commands.Checkout(repo, trackingBranch);
            }
            else
            {
                //TODO Create Tracking Branch??
                //TODO Maybe add an dialog for this?

                Branch localBranch = repo.CreateBranch(remoteBranch.RemoveRemoteFromBranchName());
                Remote remote = repo.Network.Remotes[remoteBranch.RemoteName];

                repo.Branches.Update(localBranch, 
                    b => b.Remote = remote.Name, 
                    b => b.UpstreamBranch = localBranch.CanonicalName);
            }
        }

        private Branch? GetLocalTrackingBranch(Repository repo, Branch remoteBranch)
        {
            IEnumerable<Branch> localBranches = repo.Branches.Where(b => !b.IsRemote && b.IsTracking);

            Branch? trackingBranch;

            try
            {
                trackingBranch = localBranches.Single(b =>
                    b.TrackedBranch.FriendlyName.Equals(remoteBranch.FriendlyName, StringComparison.Ordinal));
            }
            catch (InvalidOperationException)
            {
                trackingBranch = default(Branch);
            }

            return trackingBranch;
        }
    }
}
