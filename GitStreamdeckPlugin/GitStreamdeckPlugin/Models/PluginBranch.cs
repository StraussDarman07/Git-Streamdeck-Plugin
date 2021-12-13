using LibGit2Sharp;

namespace Plugin.Models
{
    public class PluginBranch
    {
        public Branch Branch { get; }

        public string DisplayName { get; }

        public PluginBranch(Branch branch)
        {
            Branch = branch;

            DisplayName = branch.IsRemote
                ? branch.FriendlyName.Replace($"{branch.RemoteName}/", string.Empty)
                : branch.FriendlyName;
        }
    }
}
