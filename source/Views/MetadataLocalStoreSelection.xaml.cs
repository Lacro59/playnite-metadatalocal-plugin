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
        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI _PlayniteApi;

        public string _PluginUserDataPath { get; set; }
        public SearchResult StoreResult { get; set; } = new SearchResult();

        public bool IsFirstLoad = true;


        public MetadataLocalStoreSelection(IPlayniteAPI PlayniteApi, string StoreDefault, string GameName, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
            _PluginUserDataPath = PluginUserDataPath;

            InitializeComponent();

            PART_DataLoadWishlist.Visibility = Visibility.Collapsed;
            PART_GridData.IsEnabled = true;

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

                case "ubisoft":
                case "uplay":
                case "ubisoft connect":
                    rbUbisoft.IsChecked = true;
                    break;

                default:
                    rbSteam.IsChecked = true;
                    break;
            }

            SearchElement.Text = GameName;

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
                RadioButton rb = sender as RadioButton;
                if (rb.Name == "rbSteam" && (bool)rb.IsChecked)
                {
                    SearchElements();
                }

                if (rb.Name == "rbEpic" && (bool)rb.IsChecked)
                {
                    SearchElements();
                }

                if (rb.Name == "rbOrigin" && (bool)rb.IsChecked)
                {
                    SearchElements();
                }

                if (rb.Name == "rbXbox" && (bool)rb.IsChecked)
                {
                    SearchElements();
                }

                if (rb.Name == "rbUbisoft" && (bool)rb.IsChecked)
                {
                    SearchElements();
                }
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
            bool IsSteam = (bool)rbSteam.IsChecked;
            bool IsOrigin = (bool)rbOrigin.IsChecked;
            bool IsEpic = (bool)rbEpic.IsChecked;
            bool IsXbox = (bool)rbXbox.IsChecked;
            bool IsUbisoft = (bool)rbUbisoft.IsChecked;

            PART_DataLoadWishlist.Visibility = Visibility.Visible;
            PART_GridData.IsEnabled = false;

            string gameSearch = RemoveAccents(SearchElement.Text);

            lbSelectable.ItemsSource = null;
            Task task = Task.Run(() => LoadData(gameSearch, IsSteam, IsOrigin, IsEpic, IsXbox, IsUbisoft))
                .ContinueWith(antecedent =>
                {
                    this.Dispatcher.Invoke(new Action(() => {
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

        private string RemoveAccents(string text)
        {
            StringBuilder sbReturn = new StringBuilder();
            var arrayText = text.Normalize(NormalizationForm.FormD).ToCharArray();
            foreach (char letter in arrayText)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                    sbReturn.Append(letter);
            }
            return sbReturn.ToString();
        }

        private async Task<List<SearchResult>> LoadData(string SearchElement, bool IsSteam, bool IsOrigin, bool IsEpic, bool IsXbox, bool IsUbisoft)
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

            if (IsUbisoft)
            {
                results = MetadataLocalProvider.GetMultiUbisoftData(_PlayniteApi, SearchElement);
            }

            return results;
        }
    }
}
