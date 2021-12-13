using LibGit2Sharp;
using Plugin.Misc;

namespace Plugin.Models
{
    public class PluginBranch
    {
        public Branch Branch { get; }

        public string DisplayName { get; }

        public PluginBranch(Branch branch)
        {
            Branch = branch;

            DisplayName = branch.RemoveRemoteFromBranchName();
        }
    }
}
