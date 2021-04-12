using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using CommonPluginsShared;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;
using System.Linq;
using MetadataLocal.OriginLibrary;
using Playnite.SDK;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using MetadataLocal.Views;
using System.Windows;
using System.Net;
using AngleSharp.Parser.Html;
using System.Web;
using MetadataLocal.Models;
using AngleSharp.Dom.Html;
using Newtonsoft.Json.Linq;
using Playnite.SDK.Data;
using MetadataLocal.EpicLibrary;
using MetadataLocal.UbisoftLibrary;
using CommonPluginsPlaynite.PluginLibrary.SteamLibrary.SteamShared;
using CommonPluginsPlaynite.PluginLibrary.OriginLibrary.Models;
using CommonPluginsPlaynite.PluginLibrary.XboxLibrary.Models;

namespace MetadataLocal
{
    public class MetadataLocalProvider : OnDemandMetadataProvider
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private MetadataLocalSettings Settings;

        private readonly MetadataRequestOptions Options;
        private readonly MetadataLocal Plugin;

        private HttpClient httpClient = new HttpClient();

        public string PlayniteConfigurationPath { get; set; }
        public static string PlayniteLanguage { get; set; }

        private string ForceStoreName = string.Empty;


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
            Options = options;
            Plugin = plugin;
            this.PlayniteConfigurationPath = PlayniteConfigurationPath;
            Settings = settings;
        }

        // Override additional methods based on supported metadata fields.
        public override string GetDescription()
        {
            // Get type source, data and description
            string Data;
            string Description = string.Empty;

            try
            {
                if (AvailableFields.Contains(MetadataField.Description))
                {
                    // Get Playnite language
                    PlayniteLanguage = Plugin.PlayniteApi.ApplicationSettings.Language;

                    string GameId = string.Empty;
                    string GameName = string.Empty;
                    string StoreName = string.Empty;

                    try
                    {
                        GameId = Options.GameData.GameId;
                        GameName = Options.GameData.Name;

                        if (Options.GameData.SourceId != default(Guid))
                        {
                            StoreName = Options.GameData.Source.Name;
                        }
                        else
                        {
                            if (ForceStoreName.IsNullOrEmpty())
                            {
                                logger.Warn("No source name");
                            }
                            else
                            {
                                StoreName = ForceStoreName;
                            }
                        }


                        // Selectable Store metadata
                        if (!Options.IsBackgroundDownload && Settings.EnableSelectStore)
                        {
                            MetadataLocalStoreSelection ViewExtension = null;
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                ViewExtension = new MetadataLocalStoreSelection(Plugin.PlayniteApi, StoreName, GameName, Plugin.GetPluginUserDataPath());
                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(Plugin.PlayniteApi, resources.GetString("LOCMetadataLocalStoreSelection"), ViewExtension);
                                windowExtension.ShowDialog();
                            }));

                            if (!ViewExtension.StoreResult.StoreName.IsNullOrEmpty())
                            {
                                GameId = ViewExtension.StoreResult.StoreId;
                                GameName = ViewExtension.StoreResult.Name;
                                StoreName = ViewExtension.StoreResult.StoreName;
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
                                uint appId = 0;
                                if (!ForceStoreName.IsNullOrEmpty())
                                {
                                    appId = (uint)new SteamApi(Plugin.GetPluginUserDataPath()).GetSteamId(GameName);
                                }
                                else
                                {
                                    appId = uint.Parse(GameId);
                                }
                                
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
                                if (!PlayniteTools.IsDisabledPlaynitePlugins("XboxLibrary", PlayniteConfigurationPath))
                                {
                                    Description = GetXboxData(GameId, PlayniteLanguage, Plugin.GetPluginUserDataPath(), Plugin).GetAwaiter().GetResult();
                                }
                                else
                                {
                                    logger.Warn("XboxLibrary is used then disabled");
                                    Plugin.PlayniteApi.Notifications.Add(new NotificationMessage(
                                        $"metadataLocal-xbox-disabled",
                                        "XboxLibrary is used then disabled",
                                        NotificationType.Error,
                                        () => Plugin.OpenSettingsView()
                                    ));
                                }
                                break;

                            case "ubisoft":
                            case "uplay":
                            case "ubisoft connect":
                                Description = GetUbisoftData(GameName, PlayniteLanguage, GameId);
                                break;

                            case "gog":
                                break;

                            default:
                                if (ForceStoreName.IsNullOrEmpty() && !(!Options.IsBackgroundDownload && Settings.EnableSelectStore))
                                {
                                    Common.LogDebug(true, "Used many stores");

                                    foreach (Store store in Settings.Stores)
                                    {
                                        ForceStoreName = store.Name;
                                        Description = GetDescription();

                                        if (!Description.IsNullOrEmpty())
                                        {
                                            Common.LogDebug(true, $"find with {ForceStoreName} for {GameName}");
                                            return Description;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error with {GameName} - {GameId} - {StoreName}");
                    }
                }

                if (Description.IsNullOrEmpty() && ForceStoreName.IsNullOrEmpty())
                {
                    return base.GetDescription();
                }
                else
                {
                    return Description;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return base.GetDescription();
            }
        }


        #region Search one to one
        public static string GetSteamData(uint appId, string PlayniteLanguage)
        {
            string url = string.Empty;
            try
            {
                string SteamLangCode = CodeLang.GetSteamLang(PlayniteLanguage);
                url = $"https://store.steampowered.com/api/appdetails?appids={appId}&l={SteamLangCode}";
                return Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to load {url}");
                return string.Empty;
            }
        }

        public static string GetOriginData(string gameId, string PlayniteLanguage)
        {
            string url = string.Empty;
            try
            {
                string OriginLang = CodeLang.GetOriginLang(PlayniteLanguage);
                string OriginLangCountry = CodeLang.GetOriginLangCountry(PlayniteLanguage);
                url = string.Format(@"https://api2.origin.com/ecommerce2/public/supercat/{0}/{1}?country={2}", gameId, OriginLang, OriginLangCountry);
                var stringData = Web.DownloadStringData(url).GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<GameStoreDataResponse>(stringData).i18n.longDescription;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to load {url}");
                return string.Empty;
            }
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
                        var page = product.pages.FirstOrDefault(a => a.type is string type && type == "productHome");
                        if (page == null)
                        {
                            page = product.pages[0];
                        }

                        Description = page.data.about.description;
                        if (!Description.IsNullOrEmpty())
                        {
                            Description = Description.Replace("\n", "\n<br>");
                            Description = Markup.MarkdownToHtml(Description);
                            Description = Regex.Replace(
                                Description,
                                "!\\[[a-zA-Z0-9- -_]*\\][\\s]*\\(((ftp|http|https):\\/\\/(\\w+:{0,1}\\w*@)?(\\S+)(:[0-9]+)?(\\/|\\/([\\w#!:.?+=&%@!\\-\\/]))?)\\)",
                                "<img src=\"$1\"/>");
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
            var tokens = Playnite.SDK.Data.Serialization.FromJsonFile<AuthorizationData>(xstsLoginTokesPath);
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
                           new StringContent(Playnite.SDK.Data.Serialization.ToJson(requestData), Encoding.UTF8, "application/json"));

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.Warn("Xbox user is not connected");
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
                    var titleHistory = Playnite.SDK.Data.Serialization.FromJson<TitleHistoryResponse>(cont);
                    return titleHistory.titles.First().detail.description;
                }
            }
        }

        public static string GetUbisoftData(string GameName, string PlayniteLanguage, string Id = "")
        {
            string Description = string.Empty;
            string url = @"https://xely3u4lod-dsn.algolia.net/1/indexes/*/queries?x-algolia-agent=Algolia%20for%20JavaScript%20(3.35.1)%3B%20Browser&x-algolia-application-id=XELY3U4LOD&x-algolia-api-key=5638539fd9edb8f2c6b024b49ec375bd";

            try
            {
                string indexName = PlayniteLanguage.Split('_')[1].ToLower() + "_release_date";
                string payload = "{\"requests\":[{\"indexName\":\"" + indexName
                    + "\",\"params\":\"ruleContexts=%5B%22web%22%5D&hitsPerPage=30&clickAnalytics=true&enableRules=true&query="
                    + GameName + "\"}]}";

                string response = Web.PostStringDataPayload(url, payload).GetAwaiter().GetResult();
                var responseObject = JsonConvert.DeserializeObject<UbisoftSearchResponse>(response);

                var ListData = responseObject.results.First().hits;
                UbisoftLibrary.GameStoreSearchResponse Data;
                if (!Id.IsNullOrEmpty() && Id.Length > 5)
                {
                    Data = ListData.Find(x => x.id == Id);
                }
                else
                {
                    Data = ListData.Find(x => Common.NormalizeGameName(x.title.ToLower()) == Common.NormalizeGameName(GameName.ToLower()));
                }

                Data.html_description.First().TryGetValue(PlayniteLanguage, out Description);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return Description;
        }
        #endregion


        #region Search one to many
        // From UniversalSteamMetadata
        public static List<SearchResult> GetMultiSteamData(string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiSteamData({searchTerm})");

            var results = new List<SearchResult>();
            string searchUrl = string.Empty;

            try
            {
                if (uint.TryParse(searchTerm, out var appId))
                {
                    using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        searchUrl = @"https://store.steampowered.com/api/appdetails?appids={0}";
                        var searchPageSrc = webClient.DownloadString(string.Format(searchUrl, appId));
                        var parsedData = JsonConvert.DeserializeObject<Dictionary<string, StoreAppDetailsResult>>(searchPageSrc);
                        var response = parsedData[appId.ToString()];

                        results.Add(new SearchResult
                        {
                            Name = response.data.name,
                            ImageUrl = response.data.header_image,
                            StoreName = "Steam",
                            StoreId = appId.ToString()
                        });
                    }
                }
                else
                {
                    using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        searchUrl = @"https://store.steampowered.com/search/?term={0}";
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
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to download {string.Format(searchUrl, searchTerm)}");
            }

            return results;
        }

        public static List<SearchResult> GetMultiOriginData(string searchTerm, string PluginUserDataPath)
        {
            Common.LogDebug(true, $"GetMultiOriginData({searchTerm})");

            string searchUrl = @"https://api1.origin.com/xsearch/store/fr_fr/fra/products?searchTerm={0}&start=0&rows=20&isGDP=true";
            var results = new List<SearchResult>();

            try
            {
                string result = Web.DownloadStringDataWithGz(string.Format(searchUrl, searchTerm)).GetAwaiter().GetResult();

                JObject resultObject = JObject.Parse(result);
                string stringData = JsonConvert.SerializeObject(resultObject["games"]["game"]);
                List<OriginLibrary.GameStoreSearchResponse> listOriginGames = JsonConvert.DeserializeObject<List<OriginLibrary.GameStoreSearchResponse>>(stringData);

                if (listOriginGames.HasItems())
                {
                    foreach (var OriginGame in listOriginGames)
                    {
                        var title = OriginGame.gameName.Trim(); ;
                        var img = OriginGame.image;

                        OriginApi originApi = new OriginApi(PluginUserDataPath);
                        string gameId = originApi.GetOriginId(title);

                        Common.LogDebug(true, $"Find for {title} - {gameId}");

                        if (!gameId.IsNullOrEmpty())
                        {
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
                Common.LogError(ex, false, $"Failed to download {string.Format(searchUrl, searchTerm)}");
            }

            return results;
        }

        public static List<SearchResult> GetMultiEpicData(string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiEpicData({searchTerm})");

            var results = new List<SearchResult>();

            try
            {
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
                Common.LogError(ex, false, $"Failed to download data for {searchTerm}");
            }

            return results;
        }

        public static List<SearchResult> GetMultiXboxData(IPlayniteAPI PlayniteApi, string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiXboxData({searchTerm})");

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
                Common.LogError(ex, false, $"Failed to download data for {searchTerm}");
            }

            return results;
        }

        public static List<SearchResult> GetMultiUbisoftData(IPlayniteAPI PlayniteApi, string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiUbisoftData({searchTerm})");

            var results = new List<SearchResult>();
            string url = @"https://xely3u4lod-dsn.algolia.net/1/indexes/*/queries?x-algolia-agent=Algolia%20for%20JavaScript%20(3.35.1)%3B%20Browser&x-algolia-application-id=XELY3U4LOD&x-algolia-api-key=5638539fd9edb8f2c6b024b49ec375bd";

            try
            {
                string PlayniteLanguage = PlayniteApi.ApplicationSettings.Language;
                string indexName = PlayniteLanguage.Split('_')[1].ToLower() + "_release_date";
                string payload = "{\"requests\":[{\"indexName\":\"" + indexName
                    + "\",\"params\":\"ruleContexts=%5B%22web%22%5D&hitsPerPage=30&clickAnalytics=true&enableRules=true&query="
                    + searchTerm + "\"}]}";

                string response = Web.PostStringDataPayload(url, payload).GetAwaiter().GetResult();
                var responseObject = JsonConvert.DeserializeObject<UbisoftSearchResponse>(response);

                var ListData = responseObject.results.First().hits;
                foreach (var game in ListData)
                {
                    string gameName = game.title;
                    string gameImg = game.additional_image_link;
                    string gameId = game.id;

                    results.Add(new SearchResult
                    {
                        Name = gameName,
                        ImageUrl = gameImg,
                        StoreName = "Ubisoft",
                        StoreId = gameId
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return results;
        }
        #endregion
    }
}
