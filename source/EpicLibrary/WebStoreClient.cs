using Playnite.SDK.Data;
using CommonPluginsShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CommonPlayniteShared.PluginLibrary.EpicLibrary.Models;
using System.Net;
using CommonPluginsShared.Extensions;

namespace MetadataLocal.EpicLibrary
{
    public class WebStoreClient : IDisposable
    {
        private HttpClient httpClient = new HttpClient();

        public const string GraphQLEndpoint = @"https://graphql.epicgames.com/graphql";
        public const string ProductUrlBase = @"https://store-content.ak.epicgames.com/api/{1}/content/products/{0}";

        public WebStoreClient()
        {
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public async Task<List<WebStoreModelsAppsList.QuerySearchResponse.SearchStoreElement>> QuerySearch(string searchTerm)
        {
            var query = new WebStoreModelsAppsList.QuerySearch();
            query.variables.keywords = HttpUtility.UrlPathEncode(searchTerm);
            var content = new StringContent(Playnite.SDK.Data.Serialization.ToJson(query), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(GraphQLEndpoint, content).ConfigureAwait(false);
            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = Playnite.SDK.Data.Serialization.FromJson<WebStoreModelsAppsList.QuerySearchResponse>(str);
            return data.data.Catalog.searchStore.elements;
        }

        public async Task<WebStoreModels.ProductResponse> GetProductInfo(string productSlug, string PlayniteLanguage)
        {
            if (!productSlug.IsNullOrEmpty())
            {
                string EpicLangCountry = CodeLang.GetEpicLangCountry(PlayniteLanguage);
                if (PlayniteLanguage == "es_ES" || PlayniteLanguage == "zh_TW")
                {
                    EpicLangCountry = CodeLang.GetEpicLang(PlayniteLanguage);
                }

                string slugUri = productSlug.Split('/').First();
                string productUrl = string.Format(ProductUrlBase, slugUri, EpicLangCountry);
                string str = string.Empty;

                try
                {
                    str = await httpClient.GetStringAsync(productUrl);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, ex.Message.Contains("404"));
                    return null;
                }

                Serialization.TryFromJson(str, out WebStoreModels.ProductResponse parsedData);
                return parsedData;
            }
            return null;
        }
    }
}
