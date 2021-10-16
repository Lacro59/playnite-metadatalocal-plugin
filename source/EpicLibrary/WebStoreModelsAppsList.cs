using CommonPlayniteShared.PluginLibrary.EpicLibrary.Models;
using System;
using System.Collections.Generic;

namespace MetadataLocal.EpicLibrary
{
    public class WebStoreModelsAppsList : WebStoreModels
    {
        public new class QuerySearchResponse
        {
            public class SearchStoreElement
            {
                public string url;
                public string title;
                public string id;
                public string productSlug;
                public List<KeyImages> keyImages;
            }

            public class KeyImages
            {
                public string type;
                public string url;
            }

            public class Data
            {
                public class CatalogItem
                {
                    public class SearchStore
                    {
                        public List<SearchStoreElement> elements;
                    }

                    public SearchStore searchStore;
                }

                public CatalogItem Catalog;
            }

            public Data data;
        }
    }
}
