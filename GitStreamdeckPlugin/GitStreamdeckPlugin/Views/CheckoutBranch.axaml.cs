using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DynamicData.Binding;
using Plugin.ViewModels;

namespace Plugin.Views
{
    public partial class CheckoutBranch : Window
    {
        public CheckoutBranch()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            TextBox inputTextBox = this.FindControl<TextBox>("InputTextBox");

            inputTextBox?.AddHandler(KeyDownEvent, InputElement_OnKeyDown, RoutingStrategies.Tunnel);

            GotFocus += OnFocus;
            Deactivated += OnLostFocus;
        }

        private void OnLostFocus(object? sender, EventArgs eventArgs)
        {
            Close();
        }

        private void OnFocus(object? sender, GotFocusEventArgs gotFocusEventArgs)
        {
            TextBox tb = this.FindControl<TextBox>("InputTextBox");
            tb.SelectionStart = 0;
            tb.SelectionEnd = int.MaxValue;
            tb.Focus();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {

            if (DataContext is BranchViewModel vm)
            {
                vm.WhenPropertyChanged(viewModel => viewModel.SelectionWatermark).Subscribe(OnWatermarkChanged);
                vm.WhenPropertyChanged(viewModel => viewModel.BranchName).Subscribe(OnBranchNameChanged);
            }

        }

        private void OnBranchNameChanged(PropertyValue<BranchViewModel, string> propertyValue)
        {
            Border border = this.FindControl<Border>("CompleteBox");
            border.IsVisible = !string.IsNullOrWhiteSpace(propertyValue.Value);
        }

        private void OnWatermarkChanged(PropertyValue<BranchViewModel, string> propertyValue)
        {
            TextBox watermarkBox = this.FindControl<TextBox>("SelectionWatermark");
            TextBox inputTextBox = this.FindControl<TextBox>("InputTextBox");

            //Reset the caret index if the selection appears again
            if (!string.IsNullOrWhiteSpace(propertyValue.Value))
                watermarkBox.CaretIndex = inputTextBox.CaretIndex - 2;

            InputElement_OnKeyDown(inputTextBox, new KeyEventArgs() { Key = Key.Right });
        }

        private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!(DataContext is BranchViewModel vm))
                return;

            if(!(sender is TextBox tb))
                return;

            switch (e.Key)
            {
                case Key.Tab:
                    bool changing = vm.OnTabPressed();
                    if (changing)
                    {
                        tb.CaretIndex = vm.BranchName.Length;
                        tb.SelectionStart = vm.BranchName.Length;
                        tb.SelectionEnd = vm.BranchName.Length;
                    }

                    e.Handled = true;
                    break;
                case Key.Down:
                    vm.OnArrowKeyStroke(true);
                    e.Handled = true;
                    break;
                case Key.Up:
                    vm.OnArrowKeyStroke(false);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    vm.OnEnterPressed();
                    Close();
                    e.Handled = true;
                    break;
                default:
                    e.Handled = false;
                    break;
            }

            //This is a fix for the underlying selection watermark
            //Magic numbers found via testing.
            //Error behaves in misaligned watermark
            TextBox watermarkBox = this.FindControl<TextBox>("SelectionWatermark");

            if (watermarkBox != null && (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Tab))
            {
                int offset = tb.CaretIndex;

                if (e.Key == Key.Right)
                    offset += 1;
                    
                else if (e.Key == Key.Left)
                    offset += -1;
                

                if (!string.IsNullOrEmpty(tb.Text) && offset >= tb.Text.Length && (e.Key == Key.Left || e.Key == Key.Right))
                    offset = tb.Text.Length - 1;

                watermarkBox.CaretIndex = offset;
            }
        }
    }
}
