using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using PluginCommon;
using Playnite.Common.Web;
using Newtonsoft.Json;
using Steam.Models;
using System.Text;
using OriginLibrary.Models;
using System.Net.Http;
using System.Linq;

namespace MetadataLocal
{
    public class MetadataLocalProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions options;
        private readonly MetadataLocal plugin;

        private HttpClient httpClient = new HttpClient();

        public string PlayniteConfigurationPath { get; set; }
        public static string  PlayniteLanguage { get; set; }

        public override List<MetadataField> AvailableFields => throw new NotImplementedException();

        public MetadataLocalProvider(MetadataRequestOptions options, MetadataLocal plugin, string PlayniteConfigurationPath)
        {
            this.options = options;
            this.plugin = plugin;
            this.PlayniteConfigurationPath = PlayniteConfigurationPath;
        }

        // Override additional methods based on supported metadata fields.
        public override string GetDescription()
        {
        // Get Playnite language
        PlayniteLanguage = Localization.GetPlayniteLanguageConfiguration(PlayniteConfigurationPath);

            // Get type source, data and description
            string Data;
            string Description = "";
            string gameId = options.GameData.GameId;
            switch (options.GameData.Source.Name.ToLower())
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
                        var catalogs = client.QuerySearch(options.GameData.Name).GetAwaiter().GetResult();
                        if (catalogs.HasItems())
                        {
                            var product = client.GetProductInfo(catalogs[0].productSlug, PlayniteLanguage).GetAwaiter().GetResult();
                            if (product.pages.HasItems())
                            {
                                ////devel7
                                //var page = product.pages[0];

                                //devel8
                                var page = product.pages.FirstOrDefault(a => a.type == "productHome");
                                if (page == null)
                                {
                                    page = product.pages[0];
                                }

                                Description = page.data.about.description;
                                if (!Description.IsNullOrEmpty())
                                {
                                    Description = Description.Replace("\n", "\n<br>");
                                }
                            }
                        }
                    }
                    break;
            }

            return Description;
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
    }
}