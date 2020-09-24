using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using PluginCommon;
using Playnite.Common.Web;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;
using System.Linq;
using MetadataLocal.OriginLibrary;
using MetadataLocal.SteamLibrary;
using Playnite.SDK;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MetadataLocal.XboxLibrary;
using Playnite.Common;
using System.IO;
using MetadataLocal.Views;
using System.Windows;
using System.Net;
using AngleSharp.Parser.Html;
using System.Web;
using MetadataLocal.Models;
using AngleSharp.Dom.Html;
using Newtonsoft.Json.Linq;

namespace MetadataLocal
{
    public class MetadataLocalProvider : OnDemandMetadataProvider
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private MetadataLocalSettings _settings;

        private readonly MetadataRequestOptions _options;
        private readonly MetadataLocal _plugin;

        private HttpClient httpClient = new HttpClient();

        public string _PlayniteConfigurationPath { get; set; }
        public static string PlayniteLanguage { get; set; }
        

        private List<MetadataField> availableFields;
        public override List<MetadataField> AvailableFields
        {
            get
            {
                if (availableFields == null)
                {
                    availableFields = GetAvailableFields();
                }
        
                return availableFields;
            }
        }

        private List<MetadataField> GetAvailableFields()
        {
            var fields = new List<MetadataField> { MetadataField.Name };
            fields.Add(MetadataField.Description);
            return fields;
        }

        public MetadataLocalProvider(MetadataRequestOptions options, MetadataLocal plugin, string PlayniteConfigurationPath, MetadataLocalSettings settings)
        {
            _options = options;
            _plugin = plugin;
            _PlayniteConfigurationPath = PlayniteConfigurationPath;
            _settings = settings;
        }

        // Override additional methods based on supported metadata fields.
        public override string GetDescription()
        {
            // Get type source, data and description
            string Data;
            string Description = string.Empty;

            if (AvailableFields.Contains(MetadataField.Description))
            {
                // Get Playnite language
                PlayniteLanguage = PluginCommon.Localization.GetPlayniteLanguageConfiguration(_PlayniteConfigurationPath);

                string GameId = string.Empty;
                string GameName = string.Empty;
                string StoreName = string.Empty;

                try
                {
                    GameId = _options.GameData.GameId;
                    GameName = _options.GameData.Name;

                    if (_options.GameData.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                    {
                        StoreName = _options.GameData.Source.Name;
                    }
                    else
                    {
                        logger.Warn("MetadataLocal - No source name");
                    }


                    // Selectable Store metadata
                    if (!_options.IsBackgroundDownload && _settings.EnableSelectStore)
                    {
                        MetadataLocalStoreSelection wpfDialog = null;
                        bool? wpfResult = null;
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            wpfDialog = new MetadataLocalStoreSelection(_plugin.PlayniteApi, StoreName, GameName, _plugin.GetPluginUserDataPath());
                            wpfResult = wpfDialog.ShowDialog();
                        }));

                        if(!wpfDialog.StoreResult.StoreName.IsNullOrEmpty())
                        {
                            GameId = wpfDialog.StoreResult.StoreId;
                            GameName = wpfDialog.StoreResult.Name;
                            StoreName = wpfDialog.StoreResult.StoreName;
                        }
                        else
                        {
                            GameId = string.Empty;
                            GameName = string.Empty;
                            StoreName = string.Empty;
                        }
                    }


                    switch (StoreName.ToLower())
                    {
                        case "steam":
                            uint appId = uint.Parse(GameId);
                            Data = GetSteamData(appId, PlayniteLanguage);
                            var parsedData = JsonConvert.DeserializeObject<Dictionary<string, StoreAppDetailsResult>>(Data);
                            Description = parsedData[appId.ToString()].data.detailed_description;
                            break;

                        case "origin":
                            Description = GetOriginData(GameId, PlayniteLanguage);
                            break;

                        case "epic":
                            Description = GetEpicData(GameName);
                            break;

                        case "xbox":
                            if (!Tools.IsDisabledPlaynitePlugins("XboxLibrary", _plugin.GetPluginUserDataPath()))
                            {
                                Description = GetXboxData(GameId, PlayniteLanguage, _plugin.GetPluginUserDataPath(), _plugin).GetAwaiter().GetResult();
                            }
                            else
                            {
                                logger.Warn("MetadataLocal - XboxLibrary is used then disabled");
                                _plugin.PlayniteApi.Notifications.Add(new NotificationMessage(
                                    $"metadataLocal-xbox-disabled",
                                    "XboxLibrary is used then disabled",
                                    NotificationType.Error,
                                    () => _plugin.OpenSettingsView()
                                ));
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "MetadataLocal", $"Error with {GameName} - {GameId} - {StoreName}");
                }
            }

            if (Description.IsNullOrEmpty())
            {
                return base.GetDescription();
            }
            else
            {
                return Description;
            }
        }


        #region Search one to one
        // Override Steam function GetRawStoreAppDetail in WebApiClient on SteamLibrary.
        public static string GetSteamData(uint appId, string PlayniteLanguage)
        {
            string SteamLangCode = CodeLang.GetSteamLang(PlayniteLanguage);
            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&l={SteamLangCode}";
            return HttpDownloader.DownloadString(url);
        }

        // Override Origin function GetGameStoreData in OriginApiClient on OriginLibrary.
        public static string GetOriginData(string gameId, string PlayniteLanguage)
        {
            string OriginLang = CodeLang.GetOriginLang(PlayniteLanguage);
            string OriginLangCountry = CodeLang.GetOriginLangCountry(PlayniteLanguage);
            var url = string.Format(@"https://api2.origin.com/ecommerce2/public/supercat/{0}/{1}?country={2}",
                gameId, OriginLang, OriginLangCountry);
            var stringData = Encoding.UTF8.GetString(HttpDownloader.DownloadData(url));
            return JsonConvert.DeserializeObject<GameStoreDataResponse>(stringData).i18n.longDescription;
        }

        public static string GetEpicData(string gameName)
        {
            using (var client = new WebStoreClient())
            {
                string Description = string.Empty;
                var catalogs = client.QuerySearch(gameName).GetAwaiter().GetResult();
                if (catalogs.HasItems())
                {
                    var product = client.GetProductInfo(catalogs[0].productSlug, PlayniteLanguage).GetAwaiter().GetResult();
                    if (product.pages.HasItems())
                    {
                        var page = product.pages.FirstOrDefault(a => a.type == "productHome");
                        if (page == null)
                        {
                            page = product.pages[0];
                        }

                        Description = page.data.about.description;
                        if (!Description.IsNullOrEmpty())
                        {
                            Description = Description.Replace("\n", "\n<br>");

                            //![rl_s1_section_challenges.jpg](https://cdn2.unrealengine.com/rl-s1-section-challenges-3840x2160-073994219.jpg)
                            // Markdown image to html image  
                            Description = Regex.Replace(
                                Description,
                                "!\\[[a-zA-Z0-9- ]*\\][\\s]*\\(((ftp|http|https):\\/\\/(\\w+:{0,1}\\w*@)?(\\S+)(:[0-9]+)?(\\/|\\/([\\w#!:.?+=&%@!\\-\\/]))?)\\)",
                                "<img src=\"$1\" width=\"100%\"/>");
                        }
                    }
                }
                return Description;
            }
        }

        // Override Xbox function GetTitleInfo in WebApiClient on XboxLibrary.
        public static async Task<string> GetXboxData(string pfn, string PlayniteLanguage, string PluginUserDataPath, MetadataLocal plugin)
        {
            var xstsLoginTokesPath = Path.Combine(PluginUserDataPath + "\\..\\7e4fbb5e-2ae3-48d4-8ba0-6b30e7a4e287", "xsts.json");
            var tokens = Serialization.FromJsonFile<AuthorizationData>(xstsLoginTokesPath);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
                client.DefaultRequestHeaders.Add("Authorization", $"XBL3.0 x={tokens.DisplayClaims.xui[0].uhs};{tokens.Token}");
                client.DefaultRequestHeaders.Add("Accept-Language", CodeLang.GetEpicLang(PlayniteLanguage));

                var requestData = new Dictionary<string, List<string>>
                {
                    { "pfns", new List<string> { pfn } },
                    { "windowsPhoneProductIds", new List<string>() },
                };

                var response = await client.PostAsync(
                           @"https://titlehub.xboxlive.com/titles/batch/decoration/detail",
                           new StringContent(Serialization.ToJson(requestData), Encoding.UTF8, "application/json"));

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.Warn("Metadatalocal - Xbox user is not connected");
                    plugin.PlayniteApi.Notifications.Add(new NotificationMessage(
                        $"metadalocal-xbox-error",
                        "Xbox - " + resources.GetString("LOCNotLoggedIn"),
                        NotificationType.Error
                    ));

                    return string.Empty;
                }
                else
                {
                    var cont = await response.Content.ReadAsStringAsync();
                    var titleHistory = Serialization.FromJson<TitleHistoryResponse>(cont);
                    return titleHistory.titles.First().detail.description;
                }
            }
        }
        #endregion


        public static async Task<string> GetData(string url)
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using (HttpClient client = new HttpClient(handler))
            {
                return client.GetStringAsync(url).GetAwaiter().GetResult();
            }
        }


        #region Search one to many
        // From UniversalSteamMetadata
        public static List<SearchResult> GetMultiSteamData(string searchTerm)
        {
#if DEBUG
            logger.Debug($"MetadataLocal - GetMultiSteamData({searchTerm})");
#endif

            string searchUrl = @"https://store.steampowered.com/search/?term={0}&category1=998";
            var results = new List<SearchResult>();

            try
            {
                using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                {
                    var searchPageSrc = webClient.DownloadString(string.Format(searchUrl, searchTerm));
                    var parser = new HtmlParser();
                    var searchPage = parser.Parse(searchPageSrc);

                    foreach (var gameElem in searchPage.QuerySelectorAll(".search_result_row"))
                    {
                        var title = gameElem.QuerySelector(".title").InnerHtml;
                        var img = gameElem.QuerySelector(".search_capsule img").GetAttribute("src");
                        var releaseDate = gameElem.QuerySelector(".search_released").InnerHtml;
                        if (gameElem.HasAttribute("data-ds-packageid"))
                        {
                            continue;
                        }
                        var gameId = gameElem.GetAttribute("data-ds-appid");

                        results.Add(new SearchResult
                        {
                            Name = HttpUtility.HtmlDecode(title),
                            ImageUrl = img,
                            StoreName = "Steam",
                            StoreId = gameId
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "MetadataLocal", $"Failed to download {string.Format(searchUrl, searchTerm)}");
            }

            return results;
        }

        public static List<SearchResult> GetMultiOriginData(string searchTerm, string PluginUserDataPath)
        {
#if DEBUG
            logger.Debug($"MetadataLocal - GetMultiOriginData({searchTerm})");
#endif
            string baseUrl = @"https://www.origin.com/";
            string listGamesUrl = @"https://api3.origin.com/supercat/FR/fr_FR/supercat-PCWIN_MAC-FR-fr_FR.json.gz";
            //string searchUrl = @"https://www.origin.com/fra/fr-fr/search?searchString={0}";
            string searchUrl = @"https://api1.origin.com/xsearch/store/fr_fr/fra/products?searchTerm={0}&start=0&rows=20&isGDP=true";
            var results = new List<SearchResult>();

            try
            {
                string result = GetData(string.Format(searchUrl, searchTerm)).GetAwaiter().GetResult();

                JObject resultObject = JObject.Parse(result);
                string stringData = JsonConvert.SerializeObject(resultObject["games"]["game"]);
                List<GameStoreSearchResponse> listOriginGames = JsonConvert.DeserializeObject<List<GameStoreSearchResponse>>(stringData);

                if (listOriginGames.HasItems())
                {
                    foreach (var OriginGame in listOriginGames)
                    {
                        var title = OriginGame.gameName.Trim(); ;
                        var img = OriginGame.image;


                        //Get Id
                        // TODO Set in PluginCommon
                        string PluginCachePath = PluginUserDataPath + "\\cache\\";
                        string PluginCacheFile = PluginCachePath + "\\OriginListApp.json";
                        List<GameStoreDataResponseAppsList> OriginListApp = new List<GameStoreDataResponseAppsList>();

                        if (!Directory.Exists(PluginCachePath))
                        {
                            Directory.CreateDirectory(PluginCachePath);
                        }

                        // From cache if it exists
                        if (File.Exists(PluginCacheFile))
                        {
                            // If not expired
                            if (File.GetLastWriteTime(PluginCacheFile).AddDays(3) > DateTime.Now)
                            {
                                OriginListApp = JsonConvert.DeserializeObject<List<GameStoreDataResponseAppsList>>(File.ReadAllText(PluginCacheFile));
                            }
                            else
                            {
                                result = GetData(string.Format(listGamesUrl)).GetAwaiter().GetResult();

                                resultObject = JObject.Parse(result);
                                stringData = JsonConvert.SerializeObject(resultObject["offers"]);
                                OriginListApp = JsonConvert.DeserializeObject<List<GameStoreDataResponseAppsList>>(stringData);
                                // Write file for cache usage
                                File.WriteAllText(PluginCacheFile, JsonConvert.SerializeObject(OriginListApp));
                            }
                        }
                        else
                        {
                            result = GetData(string.Format(listGamesUrl)).GetAwaiter().GetResult();

                            resultObject = JObject.Parse(result);
                            stringData = JsonConvert.SerializeObject(resultObject["offers"]);
                            OriginListApp = JsonConvert.DeserializeObject<List<GameStoreDataResponseAppsList>>(stringData);
                            // Write file for cache usage
                            File.WriteAllText(PluginCacheFile, JsonConvert.SerializeObject(OriginListApp));
                        }

                        var findGame = OriginListApp.Find(x => x.masterTitle.ToLower() == title.ToLower());

#if DEBUG
                        logger.Debug($"MetadataLocal - Find for {title} - {JsonConvert.SerializeObject(findGame)}");
#endif
                        if (findGame != null)
                        {
                            string gameId = findGame.offerId ?? string.Empty;

                            results.Add(new SearchResult
                            {
                                Name = title,
                                ImageUrl = img,
                                StoreName = "Origin",
                                StoreId = gameId
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "MetadataLocal", $"Failed to download {string.Format(searchUrl, searchTerm)}");
            }

            return results;
        }

        public static List<SearchResult> GetMultiEpicData(string searchTerm)
        {
#if DEBUG
            logger.Debug($"MetadataLocal - GetMultiEpicData({searchTerm})");
#endif

            var results = new List<SearchResult>();

            try { 
                using (var client = new WebStoreClient())
                {
                    var catalogs = client.QuerySearch(searchTerm).GetAwaiter().GetResult();
                    if (catalogs.HasItems())
                    {
                        foreach (var gameInfo in catalogs)
                        {
                            results.Add(new SearchResult
                            {
                                Name = gameInfo.title,
                                ImageUrl = gameInfo.keyImages.Find(x => x.type == "OfferImageWide").url,
                                StoreName = "Epic",
                                StoreId = gameInfo.id
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "MetadataLocal", $"Failed to download data for {searchTerm}");
            }

            return results;
        }

        public static List<SearchResult> GetMultiXboxData(IPlayniteAPI PlayniteApi, string searchTerm)
        {
#if DEBUG
            logger.Debug($"MetadataLocal - GetMultiXboxData({searchTerm})");
#endif

            string searchUrl = @"https://www.microsoft.com/fr-fr/search/shop/games?q={0}";
            var results = new List<SearchResult>();

            try
            {
                IWebView webView = PlayniteApi.WebViews.CreateOffscreenView();
                webView.NavigateAndWait(string.Format(searchUrl, searchTerm));
                string str = webView.GetPageSource();

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(str);

                int i = 0;
                foreach (var gameElem in htmlDocument.QuerySelectorAll("div.m-channel-placement-item"))
                {
                    if (i == 10)
                    {
                        break;
                    }

                    string gameName = gameElem.QuerySelector("h3.c-subheading-6").InnerHtml.Trim();
                    string gameImg = gameElem.QuerySelector(".c-channel-placement-image picture img").GetAttribute("src");
                    string gamePfns = gameElem.QuerySelector("a").GetAttribute("data-pfns");

                    results.Add(new SearchResult
                    {
                        Name = gameName,
                        ImageUrl = gameImg,
                        StoreName = "Xbox",
                        StoreId = gamePfns
                    });

                    i++;
                }

            }
            catch (Exception ex)
            {
                Common.LogError(ex, "MetadataLocal", $"Failed to download data for {searchTerm}");
            }

            return results;
        }
        #endregion
    }
}
