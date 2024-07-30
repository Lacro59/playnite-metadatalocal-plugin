using Playnite.SDK;
using Playnite.SDK.Data;
using CommonPluginsShared;
using MetadataLocal.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;

namespace MetadataLocal.Views
{
    /// <summary>
    /// Logique d'interaction pour MetadataLocalStoreSelection.xaml
    /// </summary>
    public partial class MetadataLocalStoreSelection : UserControl
    {
        public string PluginUserDataPath { get; set; }
        public SearchResult StoreResult { get; set; } = new SearchResult();

        public bool IsFirstLoad = true;


        public MetadataLocalStoreSelection(string storeDefault, string gameName, string pluginUserDataPath)
        {
            PluginUserDataPath = pluginUserDataPath;

            InitializeComponent();

            PART_DataLoadWishlist.Visibility = Visibility.Collapsed;
            PART_GridData.IsEnabled = true;

            switch (storeDefault.ToLower())
            {
                case "steam":
                    rbSteam.IsChecked = true;
                    break;

                case "gog":
                    rbGog.IsChecked = true;
                    break;

                case "ea app":
                case "origin":
                    rbOrigin.IsChecked = true;
                    break;

                case "epic":
                    rbEpic.IsChecked = true;
                    break;

                case "xbox":
                    rbXbox.IsChecked = true;
                    break;

                case "ubisoft":
                case "uplay":
                case "ubisoft connect":
                    rbUbisoft.IsChecked = true;
                    break;

                default:
                    rbSteam.IsChecked = true;
                    break;
            }

            SearchElement.Text = gameName;

            SearchElements();
            IsFirstLoad = false;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lbSelectable.ItemsSource = null;
            lbSelectable.UpdateLayout();
        }


        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
        }

        private void BtOk_Click(object sender, RoutedEventArgs e)
        {
            StoreResult = (SearchResult)lbSelectable.SelectedItem;
            ((Window)this.Parent).Close();
        }


        private void LbSelectable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btOk.IsEnabled = true;
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchElements();
        }
        
        private void Rb_Check(object sender, RoutedEventArgs e)
        {
            if (!IsFirstLoad)
            {
                SearchElements();
            }
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
            bool isSteam = (bool)rbSteam.IsChecked;
            bool isGog = (bool)rbGog.IsChecked;
            bool isOrigin = (bool)rbOrigin.IsChecked;
            bool isEpic = (bool)rbEpic.IsChecked;
            bool isXbox = (bool)rbXbox.IsChecked;
            bool isUbisoft = (bool)rbUbisoft.IsChecked;

            PART_DataLoadWishlist.Visibility = Visibility.Visible;
            PART_GridData.IsEnabled = false;

            string gameSearch = PlayniteTools.NormalizeGameName(SearchElement.Text);

            lbSelectable.ItemsSource = null;
            Task task = Task.Run(() => LoadData(gameSearch, isSteam, isOrigin, isEpic, isXbox, isUbisoft, isGog))
                .ContinueWith(antecedent =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        if (antecedent.Result != null)
                        {
                            lbSelectable.ItemsSource = antecedent.Result;
                        }
            
                        PART_DataLoadWishlist.Visibility = Visibility.Collapsed;
                        PART_GridData.IsEnabled = true;

                        Common.LogDebug(true, $"SearchElements({gameSearch}) - " + Serialization.ToJson(antecedent.Result));
                    }));
                });
        }

        private List<SearchResult> LoadData(string searchElement, bool isSteam, bool isOrigin, bool isEpic, bool isXbox, bool isUbisoft, bool isGog)
        {
            List<SearchResult> results = new List<SearchResult>();

            if (isSteam)
            {
                results = MetadataLocalProvider.GetMultiSteamData(searchElement);
            }

            if (isGog)
            {
                results = MetadataLocalProvider.GetMultiSGogData(searchElement);
            }

            if (isOrigin)
            {
                results = MetadataLocalProvider.GetMultiOriginData(searchElement, PluginUserDataPath);
            }

            if (isEpic)
            {
                results = MetadataLocalProvider.GetMultiEpicData(searchElement);
            }

            if (isXbox)
            {
                results = MetadataLocalProvider.GetMultiXboxData(searchElement);
            }

            if (isUbisoft)
            {
                results = MetadataLocalProvider.GetMultiUbisoftData(searchElement);
            }

            return results;
        }
    }
}
