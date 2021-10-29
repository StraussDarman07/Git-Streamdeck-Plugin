using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Elgato.StreamdeckSDK;
using Elgato.StreamdeckSDK.Types.Common;
using Plugin.Models;
using Plugin.ViewModels;
using Plugin.Views;

namespace Plugin
{
    public class GitPlugin : Application
    { 
        private GitPluginManager? PluginManager { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
               PluginManager = new GitPluginManager(desktop.Args);
            }

            base.OnFrameworkInitializationCompleted();

            PluginManager?.Initialize();
        }
    }
}
