using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using LibGit2Sharp;
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
            set => this.RaiseAndSetIfChanged(ref _branchName, value);
        }

        private SourceList<PluginBranch> Branches { get; } = new SourceList<PluginBranch>();

        private readonly ReadOnlyObservableCollection<PluginBranch> _filteredBranches;
        public ReadOnlyObservableCollection<PluginBranch> FilteredBranches => _filteredBranches;

        private PluginBranch? _selectedBranch;
        public PluginBranch? SelectedBranch
        {
            get => _selectedBranch;
            set => this.RaiseAndSetIfChanged(ref _selectedBranch, value);
        }
        
        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
        }

        public bool AreFavoritesVisible => string.IsNullOrWhiteSpace(BranchName) && FavoriteBranches.Any();
        public bool AreBranchesVisible => !string.IsNullOrWhiteSpace(BranchName);

        private const string DEFAULT_WATERMARK = "Enter Branch Name...";
        
        private string InputWatermarkText => string.IsNullOrWhiteSpace(BranchName) ? DEFAULT_WATERMARK : (Repository?.Head ?? string.Empty);
        private string SelectionWatermarkText => string.IsNullOrWhiteSpace(SelectedBranch?.DisplayName) || string.IsNullOrWhiteSpace(BranchName) ? string.Empty : (Repository?.Head ?? string.Empty);

        public string SelectionWatermark => string.IsNullOrWhiteSpace(BranchName) ? string.Empty : SelectedBranch?.DisplayName ?? string.Empty;

        public ObservableCollection<PluginBranch> FavoriteBranches { get; set; } = new ObservableCollection<PluginBranch>();

        private PluginRepository? Repository { get; set; }

        public ReactiveCommand<PluginBranch, Unit> CheckoutFavoriteBranch { get; }

        public string Head => $"({Repository?.Head})";

        public BranchViewModel()
        {
            _branchName = string.Empty;
            _selectedBranch = null;
            _isEnabled = true;

            IObservable<Func<PluginBranch, bool>> filter = this.WhenAnyValue(vm => vm.BranchName)
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
                SelectedBranch = branches.FirstOrDefault(branch => branch.DisplayName.StartsWith(input));
            });

            this.WhenPropertyChanged(vm => vm.SelectedBranch).Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SelectionWatermark));
                this.RaisePropertyChanged(nameof(SelectionWatermarkText));
            });

            this.WhenPropertyChanged(vm => vm.BranchName).Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SelectionWatermark));
                this.RaisePropertyChanged(nameof(InputWatermarkText));
                this.RaisePropertyChanged(nameof(SelectionWatermarkText));
                this.RaisePropertyChanged(nameof(AreFavoritesVisible));
                this.RaisePropertyChanged(nameof(AreBranchesVisible));
            });

            CheckoutFavoriteBranch = ReactiveCommand.Create<PluginBranch>(CheckoutBranch);
        }

        public void InitData(PluginRepository repository)
        {
            Repository = repository;

            if (!Repository.Initialized)
                Repository.Init();

            Branches.AddRange(Repository.MergedBranches);

            FavoriteBranches.AddRange(Repository.FavoriteBranches.Take(FAVORITE_COUNT));
        }

        private Func<PluginBranch, bool> BuildFilter(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return t => true;
            }

            return t => t.DisplayName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase);
        }

        public bool OnTabPressed()
        {
            bool changing = BranchName != SelectedBranch?.DisplayName;

            if (changing) 
                BranchName = SelectedBranch?.DisplayName ?? string.Empty;

            return changing;
        }

        public void OnArrowKeyStroke(bool down)
        {
            int selectedIndex = FilteredBranches.IndexOf(FilteredBranches.FirstOrDefault(branch => branch.Branch.Equals(SelectedBranch?.Branch)));

            if (down)
                selectedIndex++;
            else
                selectedIndex--;

            if (selectedIndex >= 0 && selectedIndex < FilteredBranches.Count)
                SelectedBranch = FilteredBranches[selectedIndex];
        }

        public void OnEnterPressed() => CheckoutBranch(SelectedBranch ?? throw new ArgumentException());

        private void CheckoutBranch(PluginBranch branch)
        {
            IsEnabled = false;

            Repository?.CheckoutBranch(branch);

            IsEnabled = true;

            this.RaisePropertyChanged(nameof(SelectionWatermarkText));
            this.RaisePropertyChanged(nameof(Head));
        }
    }
}
