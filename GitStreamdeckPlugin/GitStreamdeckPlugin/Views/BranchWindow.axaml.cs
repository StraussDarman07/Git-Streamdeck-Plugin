using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Plugin.Views
{
    public partial class BranchWindow : Window
    {
        public BranchWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
