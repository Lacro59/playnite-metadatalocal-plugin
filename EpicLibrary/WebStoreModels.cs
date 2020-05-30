using System;
using System.Collections.Generic;

namespace MetadataLocal
{
    public class WebStoreModels
    {
        ////devel7
        //public class QuerySearch
        //{
        //    public class Variables
        //    {
        //        public string @namespace = "epic";
        //        public string locale = CodeLang.GetEpicLang(MetadataLocalProvider.PlayniteLanguage);
        //        public string country = CodeLang.GetEpicLangCountry(MetadataLocalProvider.PlayniteLanguage).ToUpper();
        //        public string query;
        //    }
        //
        //    public Variables variables = new Variables();
        //    public string query = @"query searchQuery($namespace: String!,$locale: String!,$country: String!,$query: String!,$hasCountryFilter: Boolean,$filterCountry: String,$filterAgeGroup: Int){ Catalog {catalogOffers(namespace: $namespace, locale: $locale, params: {  keywords: $query,  country: $country}, countryAgeFilter: {shouldCheck: $hasCountryFilter,filterCountry: $filterCountry,filterAgeGroup: $filterAgeGroup}) { elements { url title id  productSlug categories { path }}}}}";
        //}

        //devel8
        public class QuerySearch
        {
            public class Variables
            {
                public string locale = CodeLang.GetEpicLang(MetadataLocalProvider.PlayniteLanguage);
                public string country = CodeLang.GetEpicLangCountry(MetadataLocalProvider.PlayniteLanguage).ToUpper();
                public string allowCountries = CodeLang.GetEpicLangCountry(MetadataLocalProvider.PlayniteLanguage).ToUpper();
                public string sortBy = "title";
                public string sortDir = "DESC";
                public string category = "games/edition/base|bundles/games|editors";
                public string keywords;
            }

            public Variables variables = new Variables();
            public string query = @"query searchStoreQuery($allowCountries: String, $category: String, $count: Int, $country: String!, $keywords: String, $locale: String, $namespace: String, $itemNs: String, $sortBy: String, $sortDir: String, $start: Int, $tag: String, $releaseDate: String, $withPrice: Boolean = false, $withPromotions: Boolean = false) {  Catalog {    searchStore(allowCountries: $allowCountries, category: $category, count: $count, country: $country, keywords: $keywords, locale: $locale, namespace: $namespace, itemNs: $itemNs, sortBy: $sortBy, sortDir: $sortDir, releaseDate: $releaseDate, start: $start, tag: $tag) {      elements {        title        id        namespace        description        effectiveDate        keyImages {          type          url        }        seller {          id          name        }        productSlug        urlSlug        url        items {          id          namespace        }        customAttributes {          key          value        }        categories {          path        }        price(country: $country) @include(if: $withPrice) {          totalPrice {            discountPrice            originalPrice            voucherDiscount            discount            currencyCode            currencyInfo {              decimals            }            fmtPrice(locale: $locale) {              originalPrice              discountPrice              intermediatePrice            }          }          lineOffers {            appliedRules {              id              endDate              discountSetting {                discountType              }            }          }        }        promotions(category: $category) @include(if: $withPromotions) {          promotionalOffers {            promotionalOffers {              startDate              endDate              discountSetting {                discountType                discountPercentage              }            }          }          upcomingPromotionalOffers {            promotionalOffers {              startDate              endDate              discountSetting {                discountType                discountPercentage              }            }          }        }      }      paging {        count        total      }    }  }}";
        }

        public class QuerySearchResponse
        {
            ////devel7
            //public class CatalogOfferElemen
            //{
            //    public string url;
            //    public string title;
            //    public string id;
            //    public string productSlug;
            //}

            //devel8
            public class SearchStoreElement
            {
                public string url;
                public string title;
                public string id;
                public string productSlug;
            }

            ////devel7
            //public class Data
            //{
            //    public class CatalogItem
            //    {
            //        public class CatalogOffer
            //        {
            //            public List<CatalogOfferElemen> elements;
            //        }
            //
            //        public CatalogOffer catalogOffers;
            //    }
            //
            //    public CatalogItem Catalog;
            //}

            //devel8
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

        public class ProductResponse
        {
            public class PageData
            {
                public class About
                {
                    public string developerAttribution;
                    public string description;
                    public string title;
                }

                public class Hero
                {
                    public string portraitBackgroundImageUrl;
                    public string backgroundImageUrl;
                }

                public Dictionary<string, string> socialLinks;
                public About about;
                public Hero hero;
            }

            ////devel7
            //public class Page
            //{
            //    public string @namespace;
            //    public string _title;
            //    public string regionBlock;
            //    public string productName;
            //    public string _urlPattern;
            //    public string _slug;
            //    public DateTime? _activeDate;
            //    public DateTime? lastModified;
            //    public string _locale;
            //    public string _id;
            //    public PageData data;
            //}

            //devel8
            public class Page
            {
                public string @namespace;
                public string _title;
                public string regionBlock;
                public string productName;
                public string _urlPattern;
                public string _slug;
                public DateTime? _activeDate;
                public DateTime? lastModified;
                public string _locale;
                public string _id;
                public PageData data;
                public string tag;
                public string type;
            }

            public string @namespace;
            public string _title;
            public string regionBlock;
            public string productName;
            public string _urlPattern;
            public string _slug;
            public DateTime? _activeDate;
            public DateTime? lastModified;
            public string _locale;
            public string _id;
            public List<Page> pages;
        }
    }
}
