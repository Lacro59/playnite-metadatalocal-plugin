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
using CommonPluginsStores.Steam;
using CommonPluginsStores.Ea;
using CommonPluginsStores.Epic;
using CommonPluginsShared.Extensions;
using AngleSharp.Dom;
using CommonPluginsStores.Gog;
using CommonPluginsStores.Models;
using CommonPluginsStores.Epic.Models.Query;
using static CommonPluginsShared.PlayniteTools;

namespace MetadataLocal
{
    public class MetadataLocalProvider : OnDemandMetadataProvider
    {
        private static ILogger Logger => LogManager.GetLogger();

        private MetadataLocalSettings Settings { get; set; }

        private MetadataRequestOptions Options { get; }
        private MetadataLocal Plugin { get; }

        public string PlayniteConfigurationPath { get; set; }
        public static string PlayniteLanguage { get; set; }

        private string ForceStoreName { get; set; } = string.Empty;


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
            List<MetadataField> fields = new List<MetadataField> { MetadataField.Name };
            fields.Add(MetadataField.Description);
            return fields;
        }

        public MetadataLocalProvider(MetadataRequestOptions options, MetadataLocal plugin, string playniteConfigurationPath, MetadataLocalSettings settings)
        {
            Options = options;
            Plugin = plugin;
            PlayniteConfigurationPath = playniteConfigurationPath;
            Settings = settings;
        }

        // Override additional methods based on supported metadata fields.
        public override string GetDescription(GetMetadataFieldArgs args)
        {
            string data = string.Empty;
            string description = string.Empty;

            try
            {
                if (AvailableFields.Contains(MetadataField.Description))
                {
                    // Get Playnite language
                    PlayniteLanguage = Plugin.PlayniteApi.ApplicationSettings.Language;

                    string gameId = string.Empty;
                    string gameName = string.Empty;
                    string storeName = ForceStoreName;
                    string storeUrl = string.Empty;

                    try
                    {
                        gameId = Options.GameData.GameId;
                        gameName = Options.GameData.Name;

                        if (Options.GameData.SourceId != default)
                        {
                            if (storeName.IsNullOrEmpty())
                            {
                                storeName = Options.GameData.Source.Name;
                            }
                        }
                        else
                        {
                            if (ForceStoreName.IsNullOrEmpty())
                            {
                                Logger.Warn("No source name");
                            }
                            else
                            {
                                storeName = ForceStoreName;
                            }
                        }


                        // Selectable Store metadata
                        if (!Options.IsBackgroundDownload && Settings.EnableSelectStore)
                        {
                            MetadataLocalStoreSelection viewExtension = null;
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                WindowOptions windowOptions = new WindowOptions
                                {
                                    CanBeResizable = false,
                                    ShowCloseButton = true,
                                    ShowMaximizeButton = false,
                                    ShowMinimizeButton = false,
                                    Height = 660,
                                    Width = 700
                                };
                                viewExtension = new MetadataLocalStoreSelection(storeName, gameName, Plugin.GetPluginUserDataPath());
                                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCMetadataLocalStoreSelection"), viewExtension, windowOptions);
                                _ = windowExtension.ShowDialog();
                            }));

                            if (!viewExtension.StoreResult.StoreName.IsNullOrEmpty())
                            {
                                gameId = viewExtension.StoreResult.StoreId;
                                gameName = viewExtension.StoreResult.Name;
                                storeName = viewExtension.StoreResult.StoreName;
                                storeUrl = viewExtension.StoreResult.StoreUrl;
                            }
                            else
                            {
                                gameId = string.Empty;
                                gameName = string.Empty;
                                storeName = string.Empty;
                                storeUrl = string.Empty;
                            }
                        }


                        switch (storeName.ToLower())
                        {
                            case "steam":
                                uint appId = 0;
                                if (!ForceStoreName.IsNullOrEmpty())
                                {
                                    SteamApi steamApi = new SteamApi("MetadataLocal", PlayniteTools.ExternalPlugin.MetadataLocal);
                                    appId = steamApi.GetAppId(Options.GameData);
                                }
                                else
                                {
                                    _ = uint.TryParse(gameId, out appId);
                                }

                                if (appId != 0)
                                {
                                    description = GetSteamData(appId, PlayniteLanguage);
                                }
                                break;

                            case "gog":
                                description = GetGogData(gameId, PlayniteLanguage);
                                break;

                            case "ea app":
                            case "origin":
                                description = GetEaData(gameId, PlayniteLanguage);
                                break;

                            case "epic":
                                description = GetEpicData(gameName);
                                break;

                            case "xbox":
                                description = GetXboxData(storeUrl);
                                break;

                            case "ubisoft":
                            case "uplay":
                            case "ubisoft connect":
                                description = GetUbisoftData(gameName, PlayniteLanguage, gameId);
                                break;

                            default:
                                if (ForceStoreName.IsNullOrEmpty() && !(!Options.IsBackgroundDownload && Settings.EnableSelectStore))
                                {
                                    Common.LogDebug(true, "Used many stores");
                                    foreach (Store store in Settings.Stores)
                                    {
                                        ForceStoreName = store.Name;
                                        description = GetDescription(args);

                                        if (!description.IsNullOrEmpty())
                                        {
                                            Common.LogDebug(true, $"Find with {ForceStoreName} for {gameName}");
                                            return description;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Error with {gameName} - {gameId} - {storeName}");
                    }
                }

                return description.IsNullOrEmpty() && ForceStoreName.IsNullOrEmpty() ? base.GetDescription(args) : description;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return base.GetDescription(args);
            }
        }


        #region Search one to one
        public static string GetSteamData(uint appId, string playniteLanguage)
        {
            try
            {
                SteamApi steamApi = new SteamApi("MetadataLocal", PlayniteTools.ExternalPlugin.MetadataLocal);
                steamApi.SetLanguage(playniteLanguage);
                GameInfos gameInfos = steamApi.GetGameInfos(appId.ToString(), null);
                return gameInfos?.Description;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        /// <summary>
        /// Loads an EA / Origin store description for a Playnite GameId (Origin offer id) via public <see cref="EaApi"/> (no login).
        /// </summary>
        /// <param name="gameId">Origin offer id from the Playnite game.</param>
        /// <param name="playniteLanguage">Playnite language code for store locale.</param>
        /// <returns>Short description, or empty string when unresolved.</returns>
        public static string GetEaData(string gameId, string playniteLanguage)
        {
            try
            {
                Common.LogDebug(true, $"GetEaData({gameId})");

                EaApi eaApi = new EaApi("MetadataLocal");
                eaApi.SetLanguage(playniteLanguage);
                GameInfos gameInfos = eaApi.GetGameInfos(gameId, null);
                string description = gameInfos?.Description;
                if (description.IsNullOrEmpty())
                {
                    Common.LogDebug(true, $"GetEaData({gameId}): no description (slug unresolved or empty drop-api payload)");
                    return string.Empty;
                }

                Common.LogDebug(true, $"GetEaData({gameId}): ok descriptionLength={description.Length}, slug='{gameInfos?.Id2}'");
                return description;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        public static string GetEpicData(string gameName)
        {
            try
            {
                Common.LogDebug(true, $"GetEpicData({gameName})");

                EpicApi epicApi = new EpicApi("MetadataLocal", ExternalPlugin.MetadataLocal);
                epicApi.SetLanguage(PlayniteLanguage);

                SearchStoreResponse response = epicApi.QuerySearchStore(gameName).GetAwaiter().GetResult();
                List<SearchStoreResponse.Element> elements = response?.Data?.Catalog?.SearchStore?.Elements;
                if (!elements.HasItems())
                {
                    Common.LogDebug(true, $"GetEpicData({gameName}): no SearchStore elements");
                    return string.Empty;
                }

                bool exactTitleMatch = true;
                SearchStoreResponse.Element catalog = elements.FirstOrDefault(a => a.Title.Equals(gameName, StringComparison.InvariantCultureIgnoreCase));
                if (catalog == null)
                {
                    exactTitleMatch = false;
                    catalog = elements[0];
                }

                string description = string.Empty;
                string descriptionSource = "none";
                if (!catalog.Namespace.IsNullOrEmpty())
                {
                    GameInfos gameInfos = epicApi.GetGameInfosAnonymous(catalog.Namespace);
                    description = gameInfos?.Description;
                    if (!description.IsNullOrEmpty())
                    {
                        descriptionSource = "GetGameInfosAnonymous";
                    }
                }

                if (description.IsNullOrEmpty())
                {
                    description = catalog.Description?.Trim() ?? string.Empty;
                    if (!description.IsNullOrEmpty())
                    {
                        descriptionSource = "SearchStore.Element";
                    }
                }

                Common.LogDebug(true,
                    $"GetEpicData({gameName}): elements={elements.Count}, selected='{catalog.Title}', " +
                    $"exactTitleMatch={exactTitleMatch}, namespace='{catalog.Namespace}', descriptionSource={descriptionSource}, " +
                    $"descriptionLength={description?.Length ?? 0}");

                if (!description.IsNullOrEmpty())
                {
                    description = description.Replace("\n", "\n<br>");
                    description = Markup.MarkdownToHtml(description);
                    description = Regex.Replace(
                        description,
                        "!\\[[a-zA-Z0-9- -_]*\\][\\s]*\\(((ftp|http|https):\\/\\/(\\w+:{0,1}\\w*@)?(\\S+)(:[0-9]+)?(\\/|\\/([\\w#!:.?+=&%@!\\-\\/]))?)\\)",
                        "<img src=\"$1\"/>");
                }

                return description;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        // Override Xbox function GetTitleInfo in WebApiClient on XboxLibrary.
        public static string GetXboxData(string url)
        {
            string description = string.Empty;

            if (!url.IsNullOrEmpty())
            {
                string webResponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                if (!webResponse.IsNullOrEmpty())
                {
                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(webResponse);

                    description = htmlDocument.QuerySelector("p#product-description")?.InnerHtml;
                    if (description.IsNullOrEmpty())
                    {
                        description = htmlDocument.QuerySelectorAll("p")
                            .Where(x => x.ClassName?.Contains("Description-module__description", StringComparison.InvariantCultureIgnoreCase) ?? false)
                            ?.FirstOrDefault()
                            ?.InnerHtml;

                        if (description.IsNullOrEmpty())
                        {
                            description = string.Empty;
                        }
                    }
                    else
                    {
                        description = description.Trim().Replace(Environment.NewLine, "<br>").Replace("\n", "<br>");
                    }
                }
            }
            else
            {
                Common.LogDebug(true, $"No url");
            }

            return description;
        }

        public static string GetUbisoftData(string gameName, string playniteLanguage, string id = "")
        {
            string description = string.Empty;
            string url = @"https://xely3u4lod-dsn.algolia.net/1/indexes/*/queries?x-algolia-agent=Algolia%20for%20JavaScript%20(3.35.1)%3B%20Browser&x-algolia-application-id=XELY3U4LOD&x-algolia-api-key=5638539fd9edb8f2c6b024b49ec375bd";

            try
            {
                string indexName = playniteLanguage.Split('_')[1].ToLower() + "_release_date";
                string payload = "{\"requests\":[{\"indexName\":\"" + indexName
                    + "\",\"params\":\"ruleContexts=%5B%22web%22%5D&hitsPerPage=30&clickAnalytics=true&enableRules=true&query="
                    + gameName.Replace("&", string.Empty).Replace("-", string.Empty).Replace(":", string.Empty) + "\"}]}";

                string response = Web.PostStringDataPayload(url, payload).GetAwaiter().GetResult();

                _ = Serialization.TryFromJson(response, out UbisoftSearchResponse parsedData);
                if (parsedData?.results?.First()?.hits == null)
                {
                    Logger.Warn($"No Ubisoft data find for {gameName}" + (id.IsNullOrEmpty() ? "" : " with {Id}"));
                    return string.Empty;
                }

                List<GameStoreSearchResponse> ListData = parsedData.results.First().hits;
                UbisoftLibrary.GameStoreSearchResponse Data;
                if (!id.IsNullOrEmpty() && id.Length > 5)
                {
                    Data = ListData.Find(x => x.id == id);
                }
                else
                {
                    Data = ListData.Find(x => PlayniteTools.NormalizeGameName(x.title.ToLower()) == PlayniteTools.NormalizeGameName(gameName.ToLower()));

                    Data = Data == null && ListData.Count == 1
                        ? ListData.First()
                        : ListData.Find(x => PlayniteTools.NormalizeGameName(x.title.ToLower()) == PlayniteTools.NormalizeGameName(gameName.Replace("&", "and").ToLower()));
                }

                _ = (Data?.html_description?.First().TryGetValue(playniteLanguage, out description));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return description;
        }

        public static string GetGogData(string gameId, string playniteLanguage)
        {
            try
            {
                GogApi gogApi = new GogApi("MetadataLocal", PlayniteTools.ExternalPlugin.MetadataLocal);
                gogApi.SetLanguage(playniteLanguage);
                GameInfos gameInfos = gogApi.GetGameInfos(gameId, null);
                string description = gameInfos?.Description;
                Common.LogDebug(true,
                    $"GetGogData({gameId}): hasDescription={!description.IsNullOrEmpty()}, name='{gameInfos?.Name}'");
                return description;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }
        #endregion


        #region Search one to many
        // From UniversalSteamMetadata
        public static List<SearchResult> GetMultiSteamData(string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiSteamData({searchTerm})");

            List<SearchResult> results = new List<SearchResult>();
            string searchUrl = string.Empty;

            try
            {
                if (uint.TryParse(searchTerm, out uint appId))
                {
                    SteamApi steamApi = new SteamApi("MetadataLocal", PlayniteTools.ExternalPlugin.MetadataLocal);
                    GameInfos gameInfos = steamApi.GetGameInfos(appId.ToString(), null);
                    if (gameInfos != null)
                    {
                        results.Add(new SearchResult
                        {
                            Name = gameInfos.Name,
                            ImageUrl = gameInfos.ImagePath,
                            StoreName = "Steam",
                            StoreId = appId.ToString()
                        });
                    }
                }
                else
                {
                    using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        searchUrl = @"https://store.steampowered.com/search/?term={0}";
                        string searchPageSrc = webClient.DownloadString(string.Format(searchUrl, searchTerm));
                        HtmlParser parser = new HtmlParser();
                        IHtmlDocument searchPage = parser.Parse(searchPageSrc);

                        foreach (IElement gameElem in searchPage.QuerySelectorAll(".search_result_row"))
                        {
                            string title = gameElem.QuerySelector(".title").InnerHtml;
                            string img = gameElem.QuerySelector(".search_capsule img").GetAttribute("src");
                            string releaseDate = gameElem.QuerySelector(".search_released").InnerHtml;
                            if (gameElem.HasAttribute("data-ds-packageid"))
                            {
                                continue;
                            }
                            string gameId = gameElem.GetAttribute("data-ds-appid");

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

        /// <summary>
        /// EA multi-search for store selection via public catalog index in <see cref="EaApi.SearchGames"/> (Phase B).
        /// </summary>
        /// <param name="searchTerm">User search term.</param>
        /// <returns>Matching EA store candidates (StoreId = Origin offer id).</returns>
        public static List<SearchResult> GetMultiEaData(string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiEaData({searchTerm})");

            List<SearchResult> results = new List<SearchResult>();
            try
            {
                EaApi eaApi = new EaApi("MetadataLocal");
                eaApi.SetLanguage(PlayniteLanguage);
                List<GameInfos> games = eaApi.SearchGames(searchTerm);
                if (games.HasItems())
                {
                    foreach (GameInfos game in games)
                    {
                        results.Add(new SearchResult
                        {
                            Name = game.Name,
                            ImageUrl = game.Image,
                            StoreName = "Origin",
                            StoreId = game.Id,
                            StoreUrl = game.Link
                        });
                    }
                }

                Common.LogDebug(true,
                    $"GetMultiEaData({searchTerm}): gamesFromApi={games?.Count ?? 0}, results={results.Count}, " +
                    $"sample=[{string.Join(", ", results.Take(3).Select(x => $"{x.Name}|{x.StoreId}"))}]");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"GetMultiEaData failed for '{searchTerm}'");
            }

            return results;
        }

        public static List<SearchResult> GetMultiEpicData(string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiEpicData({searchTerm})");

            List<SearchResult> results = new List<SearchResult>();

            try
            {
                EpicApi epicApi = new EpicApi("MetadataLocal", ExternalPlugin.MetadataLocal);
                SearchStoreResponse response = epicApi.QuerySearchStore(searchTerm).GetAwaiter().GetResult();
                List<SearchStoreResponse.Element> elements = response?.Data?.Catalog?.SearchStore?.Elements;
                if (elements.HasItems())
                {
                    foreach (SearchStoreResponse.Element gameInfo in elements)
                    {
                        string imageUrl = gameInfo.KeyImages?.Find(x => x.Type.IsEqual("OfferImageWide"))?.Url;
                        results.Add(new SearchResult
                        {
                            Name = gameInfo.Title,
                            ImageUrl = imageUrl,
                            StoreName = "Epic",
                            StoreId = gameInfo.Namespace.IsNullOrEmpty() ? gameInfo.Id : gameInfo.Namespace
                        });
                    }
                }

                Common.LogDebug(true,
                    $"GetMultiEpicData({searchTerm}): elementCount={elements?.Count ?? 0}, results={results.Count}, responseNull={response == null}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to download data for {searchTerm}");
            }

            return results;
        }

        public static List<SearchResult> GetMultiXboxData(string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiXboxData({searchTerm})");

            string searchUrl = "https://www.microsoft.com/" + CodeLang.GetEpicLang(PlayniteLanguage) + "/search/shop/games?q={0}";
            string suggestUrl = "https://www.microsoft.com/services/api/v3/suggest?market=" + CodeLang.GetEpicLang(PlayniteLanguage)
                + "&clientId=7F27B536-CF6B-4C65-8638-A0F8CBDFCA65&sources=Iris-Products%2CDCatAll-Products%2CMicrosoft-Terms&filter=%2BClientType%3AStoreWeb&counts=1%2C5%2C5&query={0}";
            List<SearchResult> results = new List<SearchResult>();


            try
            {
                string str = Web.DownloadStringDataJson(string.Format(suggestUrl, WebUtility.UrlEncode(searchTerm))).GetAwaiter().GetResult();
                MicrosoftSuggestResult microsoftSuggestResult = Serialization.FromJson<MicrosoftSuggestResult>(str);

                ResultSet data = microsoftSuggestResult?.ResultSets?.Where(x => x.Source.IsEqual("dcatall-products"))?.FirstOrDefault();
                if (data != null)
                {
                    foreach (Suggest suggest in microsoftSuggestResult?.ResultSets?.Where(x => x.Source.IsEqual("dcatall-products"))?.FirstOrDefault()?.Suggests)
                    {
                        string gameName = suggest.Title;
                        string gameImg = (suggest.ImageUrl.Contains("https:") ? string.Empty : "https:") + suggest.ImageUrl;
                        string gamePfns = string.Empty;
                        string storeUrl = (suggest.Url.Contains("https:") ? string.Empty : "https:") + suggest.Url;

                        results.Add(new SearchResult
                        {
                            Name = gameName,
                            ImageUrl = gameImg,
                            StoreName = "Xbox",
                            StoreId = gamePfns,
                            StoreUrl = storeUrl
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to download data for {searchTerm}");
            }


            try
            {
                using (IWebView webView = API.Instance.WebViews.CreateOffscreenView())
                {
                    webView.NavigateAndWait(string.Format(searchUrl, WebUtility.UrlEncode(searchTerm)));
                    string str = webView.GetPageSource();

                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(str);

                    int i = 0;
                    foreach (IElement gameElem in htmlDocument.QuerySelectorAll("#shopDetailsWrapper div.card"))
                    {
                        if (i == 10)
                        {
                            break;
                        }

                        string gameName = gameElem.QuerySelector("h3 a")?.InnerHtml?.Trim();
                        string gameImg = gameElem.QuerySelector("picture img")?.GetAttribute("src");
                        string gamePfns = gameElem.GetAttribute("data-bi-pid");
                        string storeUrl = gameElem.QuerySelector("h3 a")?.GetAttribute("href");


                        SearchResult el = results.FirstOrDefault(x => x.Name == WebUtility.HtmlDecode(gameName));
                        if (el == null)
                        {
                            results.Add(new SearchResult
                            {
                                Name = WebUtility.HtmlDecode(gameName),
                                ImageUrl = gameImg,
                                StoreName = "Xbox",
                                StoreId = gamePfns,
                                StoreUrl = storeUrl
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

        public static List<SearchResult> GetMultiUbisoftData(string searchTerm)
        {
            Common.LogDebug(true, $"GetMultiUbisoftData({searchTerm})");

            List<SearchResult> results = new List<SearchResult>();
            string url = @"https://xely3u4lod-dsn.algolia.net/1/indexes/*/queries?x-algolia-agent=Algolia%20for%20JavaScript%20(3.35.1)%3B%20Browser&x-algolia-application-id=XELY3U4LOD&x-algolia-api-key=5638539fd9edb8f2c6b024b49ec375bd";

            try
            {
                string PlayniteLanguage = API.Instance.ApplicationSettings.Language;
                string indexName = PlayniteLanguage.Split('_')[1].ToLower() + "_release_date";
                string payload = "{\"requests\":[{\"indexName\":\"" + indexName
                    + "\",\"params\":\"ruleContexts=%5B%22web%22%5D&hitsPerPage=30&clickAnalytics=true&enableRules=true&query="
                    + searchTerm + "\"}]}";

                string response = Web.PostStringDataPayload(url, payload).GetAwaiter().GetResult();
                UbisoftSearchResponse responseObject = Serialization.FromJson<UbisoftSearchResponse>(response);

                List<GameStoreSearchResponse> ListData = responseObject.results.First().hits;
                foreach (GameStoreSearchResponse game in ListData)
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
            Common.LogDebug(true, $"GetMultiSGogData({searchTerm})");

            List<SearchResult> results = new List<SearchResult>();

            string playniteLanguage = PlayniteLanguage.IsNullOrEmpty()
                ? API.Instance.ApplicationSettings.Language
                : PlayniteLanguage;
            string locale = CodeLang.GetGogLang(playniteLanguage).ToLower();
            string searchUrl = string.Format(
                "https://catalog.gog.com/v1/catalog?limit=20&locale={0}&order=desc:score&page=1&productType=in:game,pack&query=like:{1}",
                locale,
                WebUtility.UrlEncode(searchTerm));

            try
            {
                string searchData = Web.DownloadStringData(searchUrl).GetAwaiter().GetResult();
                GogSearchResult gogSearchResult = Serialization.FromJson<GogSearchResult>(searchData);
                int apiProducts = gogSearchResult?.Products?.Count ?? 0;

                foreach (GogCatalogProduct el in gogSearchResult?.Products ?? new List<GogCatalogProduct>())
                {
                    if (el.Id.IsNullOrEmpty() || el.Slug.IsNullOrEmpty())
                    {
                        continue;
                    }

                    results.Add(new SearchResult
                    {
                        Name = el.Title,
                        ImageUrl = !el.CoverVertical.IsNullOrEmpty() ? el.CoverVertical : el.CoverHorizontal,
                        StoreId = el.Id,
                        StoreName = "GOG",
                        StoreUrl = string.Format("https://www.gog.com/{0}/game/{1}", locale, el.Slug)
                    });
                }

                Common.LogDebug(true,
                    $"GetMultiSGogData({searchTerm}): locale={locale}, productCount={gogSearchResult?.ProductCount ?? 0}, apiProducts={apiProducts}, results={results.Count}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to download {searchUrl}");
            }

            return results;
        }
        #endregion
    }
}
