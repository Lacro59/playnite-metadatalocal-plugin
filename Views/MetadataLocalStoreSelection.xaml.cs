using Newtonsoft.Json;
using Playnite.Controls;
using Playnite.SDK;
using PluginCommon;
using MetadataLocal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace MetadataLocal.Views
{
    /// <summary>
    /// Logique d'interaction pour MetadataLocalStoreSelection.xaml
    /// </summary>
    public partial class MetadataLocalStoreSelection : WindowBase
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI _PlayniteApi;

        public string _PluginUserDataPath { get; set; }
        public SearchResult StoreResult { get; set; } = new SearchResult();


        public MetadataLocalStoreSelection(IPlayniteAPI PlayniteApi, string StoreDefault, string GameName, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
            _PluginUserDataPath = PluginUserDataPath;

            InitializeComponent();

            PART_DataLoadWishlist.Visibility = Visibility.Collapsed;
            PART_GridData.IsEnabled = true;

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            switch (StoreDefault.ToLower())
            {
                case "steam":
                    rbSteam.IsChecked = true;
                    break;

                case "origin":
                    rbOrigin.IsChecked = true;
                    break;

                case "epic":
                    rbEpic.IsChecked = true;
                    break;

                case "xbox":
                    rbXbox.IsChecked = true;
                    break;

                default:
                    rbSteam.IsChecked = true;
                    break;
            }

            SearchElement.Text = GameName;

            SearchElements();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Tools.DesactivePlayniteWindowControl(this);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lbSelectable.ItemsSource = null;
            lbSelectable.UpdateLayout();
        }


        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtOk_Click(object sender, RoutedEventArgs e)
        {
            StoreResult = (SearchResult)lbSelectable.SelectedItem;
            Close();
        }


        private void LbSelectable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btOk.IsEnabled = true;
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchElements();
        }

        private void SearchElement_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ButtonSearch_Click(null, null);
            }
        }


        private void SearchElements()
        {
            bool IsSteam = (bool)rbSteam.IsChecked;
            bool IsOrigin = (bool)rbOrigin.IsChecked;
            bool IsEpic = (bool)rbEpic.IsChecked;
            bool IsXbox = (bool)rbXbox.IsChecked;

            PART_DataLoadWishlist.Visibility = Visibility.Visible;
            PART_GridData.IsEnabled = false;

            string gameSearch = SearchElement.Text;

            Task task = Task.Run(() => LoadData(gameSearch, IsSteam, IsOrigin, IsEpic, IsXbox))
                .ContinueWith(antecedent =>
                {
                    Application.Current.Dispatcher.Invoke(new Action(() => {
                        lbSelectable.ItemsSource = antecedent.Result;
                        lbSelectable.UpdateLayout();
            
                        PART_DataLoadWishlist.Visibility = Visibility.Collapsed;
                        PART_GridData.IsEnabled = true;

#if DEBUG
                        logger.Debug("MetadataLocal - SearchElements() - " + JsonConvert.SerializeObject(antecedent.Result));
#endif
                    }));
                });
        }

        private async Task<List<SearchResult>> LoadData(string SearchElement, bool IsSteam, bool IsOrigin, bool IsEpic, bool IsXbox)
        {
            var results = new List<SearchResult>();

            if (IsSteam)
            {
                results = MetadataLocalProvider.GetMultiSteamData(SearchElement);
            }

            if (IsOrigin)
            {
                results = MetadataLocalProvider.GetMultiOriginData(SearchElement, _PluginUserDataPath);
            }

            if (IsEpic)
            {
                results = MetadataLocalProvider.GetMultiEpicData(SearchElement);
            }

            if (IsXbox)
            {
                results = MetadataLocalProvider.GetMultiXboxData(_PlayniteApi, SearchElement);
            }

            return results;
        }
    }
}
