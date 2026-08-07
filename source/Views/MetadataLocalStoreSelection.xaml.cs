using MetadataLocal.Models;
using MetadataLocal.ViewModels;
using CommonPluginsShared;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MetadataLocal.Views
{
    /// <summary>
    /// Store-selection dialog host. Logic lives in <see cref="MetadataLocalStoreSelectionViewModel"/>.
    /// Dialog size is defined here only — see <see cref="DialogWidth"/> / <see cref="DialogHeight"/>.
    /// </summary>
    public partial class MetadataLocalStoreSelection : UserControl
    {
        /// <summary>Default dialog width (single source of truth for window sizing).</summary>
        public const double DialogWidth = 700;

        /// <summary>Default dialog height (single source of truth for window sizing).</summary>
        public const double DialogHeight = 660;

        private readonly MetadataLocalStoreSelectionViewModel _viewModel;

        /// <summary>
        /// Creates the dialog, wires the ViewModel, and starts the initial search via the ViewModel ctor.
        /// </summary>
        /// <param name="storeDefault">Normalized or raw Playnite source / store name.</param>
        /// <param name="gameName">Initial search text.</param>
        /// <param name="pluginUserDataPath">Plugin user-data path.</param>
        public MetadataLocalStoreSelection(string storeDefault, string gameName, string pluginUserDataPath)
        {
            InitializeComponent();

            _viewModel = new MetadataLocalStoreSelectionViewModel(storeDefault, gameName, pluginUserDataPath);
            _viewModel.RequestClose += OnRequestClose;
            DataContext = _viewModel;
        }

        /// <summary>
        /// Builds <see cref="WindowOptions"/> for this dialog (size + chrome).
        /// Prefer this over hard-coding dimensions in the provider.
        /// </summary>
        /// <returns>Window options sized from <see cref="DialogWidth"/> / <see cref="DialogHeight"/>.</returns>
        public static WindowOptions CreateWindowOptions()
        {
            return new WindowOptions
            {
                CanBeResizable = false,
                ShowCloseButton = true,
                ShowMaximizeButton = false,
                ShowMinimizeButton = false,
                Width = DialogWidth,
                Height = DialogHeight,
                MinWidth = 560,
                MinHeight = 480
            };
        }

        /// <summary>Plugin user-data path from the ViewModel.</summary>
        public string PluginUserDataPath => _viewModel.PluginUserDataPath;

        /// <summary>Confirmed search result after OK; empty when cancelled.</summary>
        public SearchResult StoreResult => _viewModel.StoreResult;

        private void OnRequestClose()
        {
            Window.GetWindow(this)?.Close();
        }

        /// <summary>
        /// Runs search when Enter is pressed in the search box.
        /// </summary>
        private void SearchElement_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.SearchCommand.Execute(null);
            }
        }
    }
}
