using CommonPluginsShared;
using MetadataLocal.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MetadataLocal.ViewModels
{
    /// <summary>
    /// ViewModel for the store-selection dialog: store sidebar, multi-search results, and OK/Cancel.
    /// </summary>
    public class MetadataLocalStoreSelectionViewModel : ObservableObject
    {
        private bool _suppressStoreSearch;
        private string _searchText = string.Empty;
        private MetadataLocalStoreKind _selectedStoreKind = MetadataLocalStoreKind.Steam;
        private SearchResult _selectedResult;
        private bool _isLoading;
        private bool _isContentEnabled = true;

        /// <summary>
        /// Raised when the dialog should close (OK or Cancel).
        /// </summary>
        public event Action RequestClose;

        /// <summary>
        /// Creates the ViewModel, applies the default store, and starts the initial search.
        /// </summary>
        /// <param name="storeDefault">Normalized or raw Playnite source / store name.</param>
        /// <param name="gameName">Initial search text (usually the game name).</param>
        /// <param name="pluginUserDataPath">Plugin user-data path (kept for callers / future use).</param>
        public MetadataLocalStoreSelectionViewModel(string storeDefault, string gameName, string pluginUserDataPath)
        {
            PluginUserDataPath = pluginUserDataPath;
            StoreResult = new SearchResult();
            Stores = CreateStoreOptions();
            Results = new ObservableCollection<SearchResult>();

            SearchCommand = new RelayCommand(Search, () => !IsLoading);
            OkCommand = new RelayCommand(ConfirmSelection, () => CanConfirm);
            CancelCommand = new RelayCommand(CancelSelection);
            SelectStoreCommand = new RelayCommand<MetadataLocalStoreKind>(SelectStore);

            _searchText = gameName ?? string.Empty;

            _suppressStoreSearch = true;
            SelectedStoreKind = ResolveDefaultStore(storeDefault);
            _suppressStoreSearch = false;

            Search();
        }

        #region Properties

        /// <summary>Plugin user-data directory passed by the provider.</summary>
        public string PluginUserDataPath { get; }

        /// <summary>Confirmed result after OK; empty when cancelled.</summary>
        public SearchResult StoreResult { get; private set; }

        /// <summary>Sidebar store tiles (vertical list).</summary>
        public ObservableCollection<StoreSelectionOption> Stores { get; }

        /// <summary>Current multi-search hits for the selected store.</summary>
        public ObservableCollection<SearchResult> Results { get; }

        /// <summary>Text bound to the search box.</summary>
        public string SearchText
        {
            get => _searchText;
            set => SetValue(ref _searchText, value ?? string.Empty);
        }

        /// <summary>Currently selected store backend.</summary>
        public MetadataLocalStoreKind SelectedStoreKind
        {
            get => _selectedStoreKind;
            set
            {
                if (_selectedStoreKind == value)
                {
                    return;
                }

                SetValue(ref _selectedStoreKind, value);
                OnPropertyChanged(nameof(SelectedStoreOption));

                if (!_suppressStoreSearch)
                {
                    Search();
                }
            }
        }

        /// <summary>Selected sidebar option matching <see cref="SelectedStoreKind"/>.</summary>
        public StoreSelectionOption SelectedStoreOption
        {
            get
            {
                foreach (StoreSelectionOption option in Stores)
                {
                    if (option.Kind == SelectedStoreKind)
                    {
                        return option;
                    }
                }

                return null;
            }
            set
            {
                if (value != null)
                {
                    SelectedStoreKind = value.Kind;
                }
            }
        }

        /// <summary>Selected row in the results list.</summary>
        public SearchResult SelectedResult
        {
            get => _selectedResult;
            set
            {
                SetValue(ref _selectedResult, value);
                OnPropertyChanged(nameof(CanConfirm));
                OnPropertyChanged(nameof(IsOkEnabled));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>True while a store search is running.</summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                SetValue(ref _isLoading, value);
                OnPropertyChanged(nameof(IsLoadingVisible));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>Visibility helper for the loading overlay (<c>Visible</c>/<c>Collapsed</c>).</summary>
        public Visibility IsLoadingVisible => IsLoading ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>False while loading so the main grid can be disabled.</summary>
        public bool IsContentEnabled
        {
            get => _isContentEnabled;
            private set => SetValue(ref _isContentEnabled, value);
        }

        /// <summary>Whether OK can confirm the current selection.</summary>
        public bool CanConfirm => SelectedResult != null;

        /// <summary>Alias for binding <c>Button.IsEnabled</c> on OK.</summary>
        public bool IsOkEnabled => CanConfirm;

        #endregion

        #region Commands

        /// <summary>Runs a multi-search for the current store and search text.</summary>
        public RelayCommand SearchCommand { get; }

        /// <summary>Confirms <see cref="SelectedResult"/> and requests dialog close.</summary>
        public RelayCommand OkCommand { get; }

        /// <summary>Cancels without a store result and requests dialog close.</summary>
        public RelayCommand CancelCommand { get; }

        /// <summary>Selects a store kind (sidebar) and refreshes search.</summary>
        public RelayCommand<MetadataLocalStoreKind> SelectStoreCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Selects <paramref name="kind"/> when invoked from a command parameter.
        /// </summary>
        /// <param name="kind">Store to select.</param>
        private void SelectStore(MetadataLocalStoreKind kind)
        {
            if (kind == MetadataLocalStoreKind.Unknown)
            {
                return;
            }

            SelectedStoreKind = kind;
        }

        /// <summary>
        /// Starts an asynchronous multi-search for the current store and <see cref="SearchText"/>.
        /// </summary>
        public void Search()
        {
            IsLoading = true;
            IsContentEnabled = false;
            SelectedResult = null;
            Results.Clear();

            string gameSearch = PlayniteTools.NormalizeGameName(SearchText);
            MetadataLocalStoreKind kind = SelectedStoreKind;

            _ = Task.Run(() => LoadData(gameSearch, kind))
                .ContinueWith(antecedent =>
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        try
                        {
                            List<SearchResult> list = antecedent.Status == TaskStatus.RanToCompletion
                                ? antecedent.Result
                                : null;

                            Results.Clear();
                            if (list != null)
                            {
                                foreach (SearchResult item in list)
                                {
                                    Results.Add(item);
                                }
                            }

                            Common.LogDebug(true, $"SearchElements({gameSearch}) - " + Serialization.ToJson(list));
                        }
                        finally
                        {
                            IsLoading = false;
                            IsContentEnabled = true;
                        }
                    }));
                });
        }

        private void ConfirmSelection()
        {
            if (SelectedResult == null)
            {
                return;
            }

            StoreResult = SelectedResult;
            RequestClose?.Invoke();
        }

        private void CancelSelection()
        {
            StoreResult = new SearchResult();
            RequestClose?.Invoke();
        }

        /// <summary>
        /// Resolves the default store radio; falls back to Steam when unknown.
        /// </summary>
        /// <param name="storeDefault">Normalized or raw Playnite source / store name.</param>
        /// <returns>Store kind to pre-select.</returns>
        private static MetadataLocalStoreKind ResolveDefaultStore(string storeDefault)
        {
            MetadataLocalStoreKind resolved = MetadataLocalStoreResolver.Resolve(storeDefault);
            MetadataLocalStoreKind selected = resolved == MetadataLocalStoreKind.Unknown
                ? MetadataLocalStoreKind.Steam
                : resolved;

            Common.LogDebug(true, $"StoreSelection default: '{storeDefault}' → resolved={resolved}, selected={selected}");
            return selected;
        }

        private static ObservableCollection<StoreSelectionOption> CreateStoreOptions()
        {
            return new ObservableCollection<StoreSelectionOption>
            {
                new StoreSelectionOption(MetadataLocalStoreKind.Steam, "Steam", "\uE906"),
                new StoreSelectionOption(MetadataLocalStoreKind.Gog, "GOG", "\uEA35"),
                new StoreSelectionOption(MetadataLocalStoreKind.Epic, "Epic Games", "\uE902"),
                new StoreSelectionOption(MetadataLocalStoreKind.Ea, "EA app", "\uEA72"),
                new StoreSelectionOption(MetadataLocalStoreKind.Xbox, "Xbox", "\uE908"),
                new StoreSelectionOption(MetadataLocalStoreKind.Ubisoft, "Ubisoft Connect", "\uE907")
            };
        }

        private static List<SearchResult> LoadData(string searchElement, MetadataLocalStoreKind kind)
        {
            try
            {
                switch (kind)
                {
                    case MetadataLocalStoreKind.Steam:
                        return MetadataLocalProvider.GetMultiSteamData(searchElement) ?? new List<SearchResult>();

                    case MetadataLocalStoreKind.Gog:
                        return MetadataLocalProvider.GetMultiSGogData(searchElement) ?? new List<SearchResult>();

                    case MetadataLocalStoreKind.Ea:
                        return MetadataLocalProvider.GetMultiEaData(searchElement) ?? new List<SearchResult>();

                    case MetadataLocalStoreKind.Epic:
                        return MetadataLocalProvider.GetMultiEpicData(searchElement) ?? new List<SearchResult>();

                    case MetadataLocalStoreKind.Xbox:
                        return MetadataLocalProvider.GetMultiXboxData(searchElement) ?? new List<SearchResult>();

                    case MetadataLocalStoreKind.Ubisoft:
                        return MetadataLocalProvider.GetMultiUbisoftData(searchElement) ?? new List<SearchResult>();

                    case MetadataLocalStoreKind.Unknown:
                    default:
                        return new List<SearchResult>();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"StoreSelection LoadData failed for '{searchElement}' / {kind}");
                return new List<SearchResult>();
            }
        }

        #endregion
    }
}
