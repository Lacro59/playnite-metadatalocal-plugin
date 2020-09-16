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

namespace MetadataLocal
{
    public class MetadataLocalProvider : OnDemandMetadataProvider
    {
        private static readonly ILogger logger = LogManager.GetLogger();

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

        public MetadataLocalProvider(MetadataRequestOptions options, MetadataLocal plugin, string PlayniteConfigurationPath)
        {
            _options = options;
            _plugin = plugin;
            _PlayniteConfigurationPath = PlayniteConfigurationPath;
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
                PlayniteLanguage = Localization.GetPlayniteLanguageConfiguration(_PlayniteConfigurationPath);

                try
                {
                    string gameId = _options.GameData.GameId;

                    if (_options.GameData.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
                    {
                        switch (_options.GameData.Source.Name.ToLower())
                        {
                            case "steam":
                                uint appId = uint.Parse(gameId);
                                Data = GetSteamData(appId, PlayniteLanguage);
                                var parsedData = JsonConvert.DeserializeObject<Dictionary<string, StoreAppDetailsResult>>(Data);
                                Description = parsedData[appId.ToString()].data.detailed_description;
                                break;

                            case "origin":
                                Description = GetOriginData(gameId, PlayniteLanguage);
                                break;

                            case "epic":
                                using (var client = new WebStoreClient())
                                {
                                    var catalogs = client.QuerySearch(_options.GameData.Name).GetAwaiter().GetResult();
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

                                                // Markdown image to html image  
                                                Description = Regex.Replace(
                                                    Description,
                                                    "!\\[[a-zA-Z0-9- ]*\\][\\s]*\\(((ftp|http|https):\\/\\/(\\w+:{0,1}\\w*@)?(\\S+)(:[0-9]+)?(\\/|\\/([\\w#!:.?+=&%@!\\-\\/]))?)\\)",
                                                    "<img src=\"$1\" width=\"100%\"/>");
                                            }
                                        }
                                    }
                                }
                                break;

                            case "xbox":
                                if (!Tools.IsDisabledPlaynitePlugins("XboxLibrary", _plugin.GetPluginUserDataPath()))
                                {
                                    Description = GetXboxData(gameId, PlayniteLanguage, _plugin.GetPluginUserDataPath()).GetAwaiter().GetResult();
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
                    else
                    {
                        logger.Warn("MetadataLocal - No source name");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "MetadataLocal", $"Error with {_options.GameData.Name} - {_options.GameData.GameId} - {_options.GameData.Source.Name}");
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

        // Override Xbox function GetTitleInfo in WebApiClient on XboxLibrary.
        public static async Task<string> GetXboxData(string pfn, string PlayniteLanguage, string PluginUserDataPath)
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
                    throw new Exception("User is not authenticated.");
                }

                var cont = await response.Content.ReadAsStringAsync();
                var titleHistory = Serialization.FromJson<TitleHistoryResponse>(cont);
                return titleHistory.titles.First().detail.description;
            }
        }
    }
}
