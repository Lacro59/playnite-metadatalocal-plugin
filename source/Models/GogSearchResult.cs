using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetadataLocal.Models
{
    public class GogSearchResult
    {
        public List<Product> products { get; set; }
        public object ts { get; set; }
        public int page { get; set; }
        public int totalPages { get; set; }
        public string totalResults { get; set; }
        public int totalGamesFound { get; set; }
        public int totalMoviesFound { get; set; }
    }

    public class Video
    {
        public string id { get; set; }
        public string provider { get; set; }
    }

    public class Price
    {
        public string amount { get; set; }
        public string baseAmount { get; set; }
        public string finalAmount { get; set; }
        public bool isDiscounted { get; set; }
        public int discountPercentage { get; set; }
        public string discountDifference { get; set; }
        public string symbol { get; set; }
        public bool isFree { get; set; }
        public int discount { get; set; }
        public bool isBonusStoreCreditIncluded { get; set; }
        public string bonusStoreCreditAmount { get; set; }
        public string promoId { get; set; }
    }

    public class Availability
    {
        public bool isAvailable { get; set; }
        public bool isAvailableInAccount { get; set; }
    }

    public class FromObject
    {
        public string date { get; set; }
        public int timezone_type { get; set; }
        public string timezone { get; set; }
    }

    public class ToObject
    {
        public string date { get; set; }
        public int timezone_type { get; set; }
        public string timezone { get; set; }
    }

    public class SalesVisibility
    {
        public bool isActive { get; set; }
        public FromObject fromObject { get; set; }
        public int from { get; set; }
        public ToObject toObject { get; set; }
        public int to { get; set; }
    }

    public class WorksOn
    {
        public bool Windows { get; set; }
        public bool Mac { get; set; }
        public bool Linux { get; set; }
    }

    public class Product
    {
        public List<object> customAttributes { get; set; }
        public string developer { get; set; }
        public string publisher { get; set; }
        public List<string> gallery { get; set; }
        public Video video { get; set; }
        public List<string> supportedOperatingSystems { get; set; }
        public List<string> genres { get; set; }
        public int? globalReleaseDate { get; set; }
        public bool isTBA { get; set; }
        public Price price { get; set; }
        public bool isDiscounted { get; set; }
        public bool isInDevelopment { get; set; }
        public int id { get; set; }
        public int? releaseDate { get; set; }
        public Availability availability { get; set; }
        public SalesVisibility salesVisibility { get; set; }
        public bool buyable { get; set; }
        public string title { get; set; }
        public string image { get; set; }
        public string url { get; set; }
        public string supportUrl { get; set; }
        public string forumUrl { get; set; }
        public WorksOn worksOn { get; set; }
        public string category { get; set; }
        public string originalCategory { get; set; }
        public int? rating { get; set; }
        public int? type { get; set; }
        public bool isComingSoon { get; set; }
        public bool isPriceVisible { get; set; }
        public bool isMovie { get; set; }
        public bool isGame { get; set; }
        public string slug { get; set; }
        public bool isWishlistable { get; set; }
        public List<object> extraInfo { get; set; }
        public int? ageLimit { get; set; }
    }
}
