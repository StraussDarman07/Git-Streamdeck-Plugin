using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Plugin.Models;

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
               //desktop.MainWindow = PluginManager.CheckoutBranchWindow;
            }

            base.OnFrameworkInitializationCompleted();

            PluginManager?.Initialize();
        }
    }
}
