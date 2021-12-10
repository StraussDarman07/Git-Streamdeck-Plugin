using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Shapes;
using DynamicData;

namespace Plugin.Models
{
    public static class RepositoryUsageData
    {
        private class UsageData
        {
            public IList<RepoUsageData> UsageDataCollection { get; set; } = new List<RepoUsageData>();
        }

        private static UsageData Instance { get; } = RepositoryUsageData.Load();
        
        private struct BranchUsageData
        {
            public string Name { get; set; }

            public int Count { get; set; }

            public bool IsEmpty() => string.IsNullOrWhiteSpace(Name);

            public BranchUsageData Copy()
            {
                return new BranchUsageData {Count = Count, Name = Name};
            }
        }

        private struct RepoUsageData
        {
            public string RepositoryName { get; set; }

            public IList<BranchUsageData> BranchUsage { get; set; }

            public bool IsEmpty() => string.IsNullOrWhiteSpace(RepositoryName);
        }

        private static UsageData Load()
        {
            return new UsageData
            {
                UsageDataCollection = Misc.UsageDataHelper<RepoUsageData>.Load()
            };
        }

        public static void Save() => Misc.UsageDataHelper<RepoUsageData>.Save(Instance.UsageDataCollection);

        public static void UpdateBranch(string repository, string branch) => Instance.UpdateBranch(repository, branch);

        private static void UpdateBranch(this UsageData usageData, string repository, string branch)
        {
            RepoUsageData data = usageData.UsageDataCollection.FirstOrDefault(usageData => repository.Equals(usageData.RepositoryName, StringComparison.Ordinal));

            if (data.IsEmpty())
            {
                data = new RepoUsageData {RepositoryName = repository, BranchUsage = new List<BranchUsageData>()};
                usageData.UsageDataCollection.Add(data);
            }

            BranchUsageData branchUsageData = data.BranchUsage.FirstOrDefault(branchUsage => branch.Equals(branchUsage.Name, StringComparison.Ordinal));

            if (branchUsageData.IsEmpty())
            {
                data.BranchUsage.Add(new BranchUsageData{Count =  1, Name = branch});
            }
            else
            {
                BranchUsageData copy = branchUsageData.Copy();
                copy.Count++;

                data.BranchUsage.Replace(branchUsageData, copy);
            }

            Save();
        }

        public static IEnumerable<string> GetSortedBranch(string repository) => Instance.GetSortedBranch(repository);

        private static IEnumerable<string> GetSortedBranch(this UsageData usageData, string repository)
        {
            return usageData.UsageDataCollection
                .Select(repo => repo.BranchUsage)
                .Aggregate(new List<BranchUsageData>(), (branches1, branches2) => new List<BranchUsageData> { branches1, branches2 })
                .OrderByDescending(branch => branch.Count)
                .Select(branch => branch.Name);
        }
    }
}
