using System.Collections.Generic;
using Playnite.SDK.Data;

namespace MetadataLocal.Models
{
    /// <summary>
    /// Response from GOG public catalog search (<c>catalog.gog.com/v1/catalog</c>).
    /// </summary>
    public class GogSearchResult
    {
        /// <summary>
        /// Total number of result pages.
        /// </summary>
        [SerializationPropertyName("pages")]
        public int Pages { get; set; }

        /// <summary>
        /// Total number of products matching the query.
        /// </summary>
        [SerializationPropertyName("productCount")]
        public int ProductCount { get; set; }

        /// <summary>
        /// Number of products included in the current response page.
        /// </summary>
        [SerializationPropertyName("currentlyShownProductCount")]
        public int CurrentlyShownProductCount { get; set; }

        /// <summary>
        /// Catalog products for the current page.
        /// </summary>
        [SerializationPropertyName("products")]
        public List<GogCatalogProduct> Products { get; set; }
    }

    /// <summary>
    /// Product entry returned by GOG catalog search.
    /// </summary>
    public class GogCatalogProduct
    {
        /// <summary>
        /// GOG product identifier (string in catalog API).
        /// </summary>
        [SerializationPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Store page slug (used to build the product URL).
        /// </summary>
        [SerializationPropertyName("slug")]
        public string Slug { get; set; }

        /// <summary>
        /// Localized product title.
        /// </summary>
        [SerializationPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Horizontal cover image URL.
        /// </summary>
        [SerializationPropertyName("coverHorizontal")]
        public string CoverHorizontal { get; set; }

        /// <summary>
        /// Vertical cover image URL.
        /// </summary>
        [SerializationPropertyName("coverVertical")]
        public string CoverVertical { get; set; }

        /// <summary>
        /// Release date string as returned by the catalog (e.g. <c>yyyy.MM.dd</c>).
        /// </summary>
        [SerializationPropertyName("releaseDate")]
        public string ReleaseDate { get; set; }

        /// <summary>
        /// Product type (<c>game</c>, <c>pack</c>, …).
        /// </summary>
        [SerializationPropertyName("productType")]
        public string ProductType { get; set; }
    }
}
