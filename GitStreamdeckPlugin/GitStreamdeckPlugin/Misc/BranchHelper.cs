using System;
using System.Collections.Generic;
using System.Text;
using LibGit2Sharp;

namespace Plugin.Misc
{
    public static class BranchHelper
    {
        public static string RemoveRemoteFromBranchName(this Branch branch) 
            => branch.IsRemote ? branch.FriendlyName.Replace($"{branch.FriendlyName}/", string.Empty) : branch.FriendlyName;
        
    }
}
