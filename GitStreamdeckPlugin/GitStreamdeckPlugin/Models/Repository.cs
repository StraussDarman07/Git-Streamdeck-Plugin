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

        public IEnumerable<Branch> LocalBranches { get; private set; } = new List<Branch>();
        public IEnumerable<Branch> RemoteBranches { get; private set; } = new List<Branch>();

        public IEnumerable<Branch> FavoriteBranches { get; private set; } = new List<Branch>();

        public bool Initialized { get; private set; }

        public PluginRepository(string path)
        {
            RepositoryPath = path;
        }

        public void Init()
        {
            using var repo = new Repository(RepositoryPath);

            //Commands.Fetch(repo);

            UpdateRepository(repo);

            Initialized = true;
        }

        private void UpdateRepository(Repository repo)
        {
            Head = repo.Head.FriendlyName;

            LocalBranches = LoadLocalBranches(repo);

            RemoteBranches = LoadRemoteBranches(repo);

            FavoriteBranches = LoadFavoriteBranches(repo);
        }

        private static IEnumerable<Branch> LoadLocalBranches(Repository repository)
            => repository.Branches.Where(b => !b.IsRemote).ToList();

        private static IEnumerable<Branch> LoadRemoteBranches(Repository repository)
            => repository.Branches.Where(b => b.IsRemote).ToList();

        private static IEnumerable<Branch> LoadFavoriteBranches(Repository repository)
        {
            IEnumerable<string> favoriteBranches = RepositoryUsageData.GetSortedBranch(repository.Info.WorkingDirectory);

            return LoadLocalBranches(repository).Where(branch => favoriteBranches.Any(fav => branch.FriendlyName.Equals(fav, StringComparison.Ordinal)));
        }

        public void CheckoutBranch(string branchName)
        {
            using var repo = new Repository(RepositoryPath);

            Branch branch = repo.Branches.FirstOrDefault(b => b.FriendlyName.Equals(branchName, StringComparison.OrdinalIgnoreCase)) ??
                throw new InvalidOperationException();

            if (branch.IsRemote)
                HandleRemoteBranchCheckout(repo, branch);

            Branch newBranch = Commands.Checkout(repo, branch, new CheckoutOptions
            {
                CheckoutModifiers = CheckoutModifiers.None, 
                CheckoutNotifyFlags = CheckoutNotifyFlags.None,
                OnCheckoutNotify = (path, flags) =>
                {
                    if (flags == CheckoutNotifyFlags.Conflict)
                    {
                        throw new Exception("Conflict");
                    }

                    return true;
                },

            });

            UpdateRepository(repo);

            RepositoryUsageData.UpdateBranch(repo.Info.WorkingDirectory, branchName);
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
