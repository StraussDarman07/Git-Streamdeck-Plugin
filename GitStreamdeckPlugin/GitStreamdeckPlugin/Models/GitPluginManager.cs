using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using Elgato.StreamdeckSDK;
using Elgato.StreamdeckSDK.Types.Common;
using Elgato.StreamdeckSDK.Types.Events;
using Plugin.ViewModels;
using Plugin.Views;

namespace Plugin.Models
{
    public class GitPluginManager
    {
        private ESDConnectionManager ConnectionManager { get; }

        private Task EventLoop { get; set; }

        public GitPluginManager(string[] args)
        {
            ESDAppArguments arguments = ESDAppArguments.Parse(args);
            ConnectionManager = new ESDConnectionManager(arguments.Port, arguments.PluginUUID, arguments.RegisterEvent);
        }

        public void Initialize()
        {
            EventLoop = ConnectionManager.Run();
            
            ConnectionManager.KeyDownForAction += (_, notification) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    new BranchWindow{DataContext = new BranchViewModel()}.Show();
                });
            };
        }
    }
}
