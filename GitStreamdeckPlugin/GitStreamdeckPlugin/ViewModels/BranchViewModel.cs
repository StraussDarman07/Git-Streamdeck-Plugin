using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Threading;
using DynamicData;
using Plugin.Models;

namespace Plugin.ViewModels
{
    public class BranchViewModel : ViewModelBase
    {
        private const int FAVORITE_COUNT = 3;

        private string _branchName;
        public string BranchName
        {
            get => _branchName;
            set
            {
                this.RaiseAndSetIfChanged(ref _branchName, value);
                this.RaisePropertyChanged(nameof(SelectionWatermark));
                this.RaisePropertyChanged(nameof(InputWatermarkText));
                this.RaisePropertyChanged(nameof(SelectionWatermarkText));
                this.RaisePropertyChanged(nameof(AreFavoritesVisible));
                this.RaisePropertyChanged(nameof(AreBranchesVisible));
            }
        }

        private SourceList<string> Branches { get; } = new SourceList<string>();

        private readonly ReadOnlyObservableCollection<string> _filteredBranches;
        public ReadOnlyObservableCollection<string> FilteredBranches => _filteredBranches;

        private string _selectedBranchName;
        public string SelectedBranchName
        {
            get => _selectedBranchName;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedBranchName, value);
                this.RaisePropertyChanged(nameof(SelectionWatermark));
                this.RaisePropertyChanged(nameof(SelectionWatermarkText));
            }
        }
        
        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
        }

        public bool AreFavoritesVisible => string.IsNullOrWhiteSpace(BranchName);
        public bool AreBranchesVisible => !AreFavoritesVisible;

        private const string DEFAULT_WATERMARK = "Enter Branch Name...";
        
        private string InputWatermarkText => string.IsNullOrWhiteSpace(BranchName) ? DEFAULT_WATERMARK : (Repository?.Head ?? string.Empty);
        private string SelectionWatermarkText => string.IsNullOrWhiteSpace(SelectedBranchName) || string.IsNullOrWhiteSpace(BranchName) ? string.Empty : (Repository?.Head ?? string.Empty);

        public string SelectionWatermark => string.IsNullOrWhiteSpace(BranchName) ? string.Empty : SelectedBranchName;

        public ObservableCollection<string> FavoriteBranches { get; set; } = new ObservableCollection<string>();

        private PluginRepository? Repository { get; set; }

        public ReactiveCommand<string, Unit> CheckoutFavoriteBranch { get; }

        public string Head => $"({Repository.Head})";
        
        public BranchViewModel()
        {
            _branchName = string.Empty;
            _selectedBranchName = string.Empty;
            _isEnabled = true;

            IObservable<Func<string, bool>> filter = this.WhenAnyValue(vm => vm.BranchName)
                .Select(BuildFilter);

            Branches.Connect()
                .Filter(filter)
                .ObserveOn(AvaloniaScheduler.Instance)
                .Bind(out _filteredBranches)
                .Subscribe();

            this.WhenAnyValue(vm => vm.BranchName, vm => vm.FilteredBranches).Subscribe(x =>
            {
                var (input, branches) = x;
                if (input == null || branches == null)
                    return;
                SelectedBranchName = branches.FirstOrDefault(branch => branch.StartsWith(input)) ?? string.Empty;
            });

            CheckoutFavoriteBranch = ReactiveCommand.Create<string>(CheckoutBranch);
        }

        public void InitData(PluginRepository repository)
        {
            Repository = repository;

            if (!Repository.Initialized)
                Repository.Init();

            Branches.AddRange(Repository.LocalBranches);

            FavoriteBranches.AddRange(Repository.FavoriteBranches.Take(FAVORITE_COUNT));
        }

        private Func<string, bool> BuildFilter(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return t => true;
            }

            return t => t.StartsWith(searchText, StringComparison.OrdinalIgnoreCase);
        }

        public bool OnTabPressed()
        {
            bool changing = BranchName != SelectedBranchName;

            if (changing) 
                BranchName = SelectedBranchName;

            return changing;
        }

        public void OnArrowKeyStroke(bool down)
        {
            int selectedIndex = FilteredBranches.IndexOf(SelectedBranchName);

            if (down)
                selectedIndex++;
            else
                selectedIndex--;

            if (selectedIndex >= 0 && selectedIndex < FilteredBranches.Count)
                SelectedBranchName = FilteredBranches[selectedIndex];
        }

        public void OnEnterPressed() => CheckoutBranch(SelectedBranchName);

        private void CheckoutBranch(string branch)
        {
            IsEnabled = false;

            Repository?.CheckoutBranch(branch);

            IsEnabled = true;

            this.RaisePropertyChanged(nameof(SelectionWatermarkText));
            this.RaisePropertyChanged(nameof(Head));
        }
    }
}
