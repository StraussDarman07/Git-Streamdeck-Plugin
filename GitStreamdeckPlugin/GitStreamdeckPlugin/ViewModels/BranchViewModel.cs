using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Avalonia.Animation;
using ReactiveUI;
using Avalonia.Threading;
using DynamicData;
using LibGit2Sharp;

namespace Plugin.ViewModels
{
    public class BranchViewModel : ViewModelBase
    {
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

        private const string DefaultWatermark = "Enter Branch Name...";

        private string Head { get; set; }
        private string InputWatermarkText => string.IsNullOrWhiteSpace(BranchName) ? DefaultWatermark : Head;
        private string SelectionWatermarkText => string.IsNullOrWhiteSpace(SelectedBranchName) || string.IsNullOrWhiteSpace(BranchName) ? string.Empty : Head;

        public string SelectionWatermark => string.IsNullOrWhiteSpace(BranchName) ? string.Empty : SelectedBranchName;

        public ObservableCollection<string> TestCollection { get; set; } = new ObservableCollection<string>();

        public BranchViewModel()
        {
            _branchName = string.Empty;
            _selectedBranchName = string.Empty;
            _isEnabled = true;
            Head = string.Empty;

            InitData();
            TestCollection.AddRange(GetLocalBranches());


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
        }

        private void InitData()
        {
            Branches.AddRange(GetLocalBranches());
        }

        public void ResetData()
        {
            Branches.Edit(list =>
            {
                list.Clear();
                list.AddRange(GetLocalBranches());
            });
        }

        private IEnumerable<string> GetLocalBranches()
        {
            using var repo = new Repository(@"C:\Users\thomas.stachl\Projects\video-hub");
            Head = repo.Head.FriendlyName;
            return repo.Branches.Where(b => !b.IsRemote).Select(b => b.FriendlyName).ToList();
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

        public void OnEnterPressed()
        {
            IsEnabled = false;
            using var repo = new Repository(@"C:\Users\thomas.stachl\Projects\video-hub");
            Branch branch = repo.Branches.FirstOrDefault(b => b.FriendlyName.Equals(SelectedBranchName, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException();

            Commands.Checkout(repo, branch);
            IsEnabled = true;
            Head = repo.Head.FriendlyName;
            this.RaisePropertyChanged(nameof(Head));
            this.RaisePropertyChanged(nameof(SelectionWatermarkText));
        }
    }
}
