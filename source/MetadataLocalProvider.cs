using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using CommonPluginsShared;
using System.Text;
using System.Linq;
using Playnite.SDK;
using System.Text.RegularExpressions;
using MetadataLocal.Views;
using System.Windows;
using System.Net;
using AngleSharp.Parser.Html;
using System.Web;
using MetadataLocal.Models;
using AngleSharp.Dom.Html;
using Playnite.SDK.Data;
using MetadataLocal.UbisoftLibrary;
using CommonPlayniteShared.PluginLibrary.SteamLibrary.SteamShared;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Models;
using CommonPlayniteShared.PluginLibrary.GogLibrary.Models;
using CommonPluginsStores.Steam;
using CommonPluginsStores.Origin;
using CommonPluginsShared.Extensions;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Services;

namespace MetadataLocal
{
    public class MetadataLocalProvider : OnDemandMetadataProvider
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private MetadataLocalSettings Settings;

        private readonly MetadataRequestOptions Options;
        private readonly MetadataLocal Plugin;

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
        public override string GetDescription(GetMetadataFieldArgs args)
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
                    string StoreName = ForceStoreName;
                    string StoreUrl = string.Empty;

                    try
                    {
                        GameId = Options.GameData.GameId;
                        GameName = Options.GameData.Name;

                        if (Options.GameData.SourceId != default(Guid))
                        {
                            if (StoreName.IsNullOrEmpty())
                            {
                                StoreName = Options.GameData.Source.Name;
                            }
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
                                StoreUrl = ViewExtension.StoreResult.StoreUrl;
                            }
                            else
                            {
                                GameId = string.Empty;
                                GameName = string.Empty;
                                StoreName = string.Empty;
                                StoreUrl = string.Empty;
                            }
                        }


                        switch (StoreName.ToLower())
                        {
                            case "steam":
                                uint appId = 0;
                                if (!ForceStoreName.IsNullOrEmpty())
                                {
                                    appId = (uint)new SteamApi("MetadataLocal").GetAppId(GameName);
                                }
                                else
                                {
                                    uint.TryParse(GameId, out appId);
                                }

                                if (appId != 0)
                                {
                                    Data = GetSteamData(appId, PlayniteLanguage);
                                    Serialization.TryFromJson(Data, out Dictionary<string, StoreAppDetailsResult> parsedData);

                                    if (parsedData != null)
                                    {
                                        Description = parsedData[appId.ToString()]?.data?.about_the_game;
                                    }
                                    else
                                    {
                                        Common.LogDebug(true, $"No Steam data find for {GameName} with {appId}");
                                    }
                                }
                                break;
                        
                            case "gog":
                                Data = GetGogData(GameId, PlayniteLanguage);
                                Serialization.TryFromJson(Data, out ProductApiDetail gogParsedData);
                                
                                if (gogParsedData != null)
                                {
                                    Description = gogParsedData.description?.full;
                                }
                                else
                                {
                                    Common.LogDebug(true, $"No GOG data find for {GameName} with {GameId}");
                                }
                                break;

                            case "ea app":
                            case "origin":
                                Description = GetOriginData(GameId, PlayniteLanguage);
                                break;

                            case "epic":
                                Description = GetEpicData(GameName);
                                break;

                            case "xbox":
                                Description = GetXboxData(StoreUrl);
                                break;

                            case "ubisoft":
                            case "uplay":
                            case "ubisoft connect":
                                Description = GetUbisoftData(GameName, PlayniteLanguage, GameId);
                                break;

                            default:
                                if (ForceStoreName.IsNullOrEmpty() && !(!Options.IsBackgroundDownload && Settings.EnableSelectStore))
                                {
                                    Common.LogDebug(true, "Used many stores");
                                    foreach (Store store in Settings.Stores)
                                    {
                                        ForceStoreName = store.Name;
                                        Description = GetDescription(args);

                                        if (!Description.IsNullOrEmpty())
                                        {
                                            Common.LogDebug(true, $"Find with {ForceStoreName} for {GameName}");
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
                    return base.GetDescription(args);
                }
                else
                {
                    return Description;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return base.GetDescription(args);
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

                Serialization.TryFromJson(stringData, out GameStoreDataResponse parsedData);
                if (parsedData?.i18n?.longDescription != null)
                {
                    return parsedData.i18n.longDescription;
                }
                else
                {
                    Common.LogDebug(true, $"No Origin data find with {gameId}");
                    return string.Empty;
                }
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
                if (catalogs?.HasItems() ?? false)
                {
                    var catalog = catalogs.FirstOrDefault(a => a.title.Equals(gameName, StringComparison.InvariantCultureIgnoreCase));
                    if (catalog == null)
                    {
                        catalog = catalogs[0];
                    }

                    var product = client.GetProductInfo(catalog.productSlug, PlayniteLanguage).GetAwaiter().GetResult();
                    if (product?.pages?.HasItems() ?? false)
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
        public static string GetXboxData(string Url)
        {
            string Description = string.Empty;

            if (!Url.IsNullOrEmpty())
            {
                string WebResponse = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                if (!WebResponse.IsNullOrEmpty())
                {
                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(WebResponse);

                    Description = htmlDocument.QuerySelector("p#product-description")?.InnerHtml;
                    if (Description.IsNullOrEmpty())
                    {
                        Description = htmlDocument.QuerySelectorAll("p")
                            .Where(x => x.ClassName?.Contains("Description-module__description", StringComparison.InvariantCultureIgnoreCase) ?? false)
                            ?.FirstOrDefault()
                            ?.InnerHtml;

                        if (Description.IsNullOrEmpty())
                        {
                            Description = string.Empty;
                        }
                    }
                    else
                    {
                        Description = Description.Trim().Replace(Environment.NewLine, "<br>").Replace("\n", "<br>");
                    }
                }
            }
            else
            {
                Common.LogDebug(true, $"No url");
            }

            return Description;
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
                    + GameName.Replace("&", string.Empty).Replace("-", string.Empty).Replace(":", string.Empty) + "\"}]}";

                string response = Web.PostStringDataPayload(url, payload).GetAwaiter().GetResult();

                Serialization.TryFromJson(response, out UbisoftSearchResponse parsedData);
                if (parsedData?.results?.First()?.hits == null)
                {
                    logger.Warn($"No Ubisoft data find for {GameName}" + (Id.IsNullOrEmpty() ? "" : " with {Id}"));
                    return string.Empty;
                }

                var ListData = parsedData.results.First().hits;
                UbisoftLibrary.GameStoreSearchResponse Data;
                if (!Id.IsNullOrEmpty() && Id.Length > 5)
                {
                    Data = ListData.Find(x => x.id == Id);
                }
                else
                {
                    Data = ListData.Find(x => CommonPluginsShared.PlayniteTools.NormalizeGameName(x.title.ToLower()) == CommonPluginsShared.PlayniteTools.NormalizeGameName(GameName.ToLower()));

                    if (Data == null && ListData.Count == 1)
                    {
                        Data = ListData.First();
                    }
                    else
                    {
                        Data = ListData.Find(x => CommonPluginsShared.PlayniteTools.NormalizeGameName(x.title.ToLower()) == CommonPluginsShared.PlayniteTools.NormalizeGameName(GameName.Replace("&", "and").ToLower()));
                    }
                }

                Data?.html_description?.First().TryGetValue(PlayniteLanguage, out Description);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return Description;
        }

        public static string GetGogData(string gameId, string PlayniteLanguage)
        {
            string url = @"http://api.gog.com/products/{0}?expand=description";
            string UrlGogLang = @"https://www.gog.com/user/changeLanguage/{0}";

            try
            {
                string GogLangCode = CodeLang.GetGogLang(PlayniteLanguage);
                string UrlLang = string.Format(UrlGogLang, GogLangCode.ToLower());
                return Web.DownloadStringDataWithUrlBefore(string.Format(url, gameId), UrlLang).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to load {url}");
                return string.Empty;
            }
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
                        var parsedData = Serialization.FromJson<Dictionary<string, StoreAppDetailsResult>>(searchPageSrc);
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

                            if (!gameId.IsNullOrEmpty())
                            {
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

                dynamic resultObject = Serialization.FromJson<dynamic>(result);
                string stringData = Serialization.ToJson(resultObject["games"]["game"]);
                List<OriginLibrary.GameStoreSearchResponse> listOriginGames = Serialization.FromJson<List<OriginLibrary.GameStoreSearchResponse>>(stringData);

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

            string searchUrl = "https://www.microsoft.com/" + CodeLang.GetEpicLang(PlayniteLanguage) + "/search/shop/games?q={0}";
            string suggestUrl = "https://www.microsoft.com/services/api/v3/suggest?market=" + CodeLang.GetEpicLang(PlayniteLanguage) 
                + "&clientId=7F27B536-CF6B-4C65-8638-A0F8CBDFCA65&sources=Iris-Products%2CDCatAll-Products%2CMicrosoft-Terms&filter=%2BClientType%3AStoreWeb&counts=1%2C5%2C5&query={0}";
            var results = new List<SearchResult>();


            try
            {
                string str = Web.DownloadStringDataJson(string.Format(suggestUrl, WebUtility.UrlEncode(searchTerm))).GetAwaiter().GetResult();
                MicrosoftSuggestResult microsoftSuggestResult = Serialization.FromJson<MicrosoftSuggestResult>(str);

                foreach (var suggest in microsoftSuggestResult?.ResultSets?.Where(x => x.Source.IsEqual("dcatall-products"))?.FirstOrDefault()?.Suggests)
                {
                    string gameName = suggest.Title;
                    string gameImg = (suggest.ImageUrl.Contains("https:") ? string.Empty :"https:") + suggest.ImageUrl;
                    string gamePfns = string.Empty;
                    string StoreUrl = (suggest.Url.Contains("https:") ? string.Empty : "https:") + suggest.Url;

                    results.Add(new SearchResult
                    {
                        Name = gameName,
                        ImageUrl = gameImg,
                        StoreName = "Xbox",
                        StoreId = gamePfns,
                        StoreUrl = StoreUrl
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to download data for {searchTerm}");
            }


            try
            {
                using (IWebView webView = PlayniteApi.WebViews.CreateOffscreenView())
                {
                    webView.NavigateAndWait(string.Format(searchUrl, WebUtility.UrlEncode(searchTerm)));
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

                        string gameName = gameElem.QuerySelector("h3.c-subheading-6")?.InnerHtml?.Trim();
                        string gameImg = gameElem.QuerySelector(".c-channel-placement-image picture img")?.GetAttribute("src");
                        string gamePfns = gameElem.QuerySelector("a")?.GetAttribute("data-pfns");
                        string StoreUrl = "https://www.microsoft.com" + gameElem.QuerySelector("a")?.GetAttribute("href");


                        var el = results.Where(x => x.Name == WebUtility.HtmlDecode(gameName)).FirstOrDefault();
                        if (el == null)
                        {
                            results.Add(new SearchResult
                            {
                                Name = WebUtility.HtmlDecode(gameName),
                                ImageUrl = gameImg,
                                StoreName = "Xbox",
                                StoreId = gamePfns,
                                StoreUrl = StoreUrl
                            });

                            i++;
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
                var responseObject = Serialization.FromJson<UbisoftSearchResponse>(response);

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

        public static List<SearchResult> GetMultiSGogData(string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiSteamData({searchTerm})");

            var results = new List<SearchResult>();
            string searchUrl = "https://www.gog.com/games/ajax/filtered?limit=20&search={0}";
            searchUrl = string.Format(searchUrl, WebUtility.UrlEncode(searchTerm));

            try
            {
                string searchData = Web.DownloadStringData(searchUrl).GetAwaiter().GetResult();
                GogSearchResult gogSearchResult = Serialization.FromJson<GogSearchResult>(searchData);

                foreach (var el in gogSearchResult?.products)
                {
                    results.Add(new SearchResult
                    {
                        Name = el.title,
                        ImageUrl = "https:" + el.image + "_200.jpg",
                        StoreId = el.id.ToString(),
                        StoreName = "GOG",
                        StoreUrl = "https://www.gog.com/" + el.url
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to download {string.Format(searchUrl, searchTerm)}");
            }

            return results;
        }
        #endregion
    }
}
