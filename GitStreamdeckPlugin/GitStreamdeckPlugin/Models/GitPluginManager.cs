using Avalonia.Controls;
using Avalonia.Threading;
using Elgato.StreamdeckSDK;
using Elgato.StreamdeckSDK.Types.Common;
using Plugin.ViewModels;
using Plugin.Views;

namespace Plugin.Models
{
    public class GitPluginManager
    {
        private ESDConnectionManager ConnectionManager { get; }

        private BranchViewModel BranchViewModel { get; } = new BranchViewModel();

        public Window CheckoutBranchWindow => new CheckoutBranch {DataContext = BranchViewModel };

        public GitPluginManager(string[] args)
        {
            ESDAppArguments arguments = ESDAppArguments.Parse(args);
            ConnectionManager = new ESDConnectionManager(arguments.Port, arguments.PluginUUID, arguments.RegisterEvent);
        }

        public void Initialize()
        {
            ConnectionManager.Run().ConfigureAwait(false);
            
            ConnectionManager.KeyDownForAction += (_, notification) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Window window = CheckoutBranchWindow;
                    window.Topmost = true;
                    window.Show();
                    window.Activate();
                    window.Topmost = false;
                });
            };
        }
    }
}
