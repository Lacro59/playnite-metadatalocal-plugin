using System.Collections.Generic;

namespace MetadataLocal.UbisoftLibrary
{
    public class UbisoftSearchResponse
    {
        public List<Hits> results { get; set; }
    }

    public class Hits
    {
        public List<GameStoreSearchResponse> hits { get; set; }
    }

    public class GameStoreSearchResponse
    {
        public string id { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public string image_link { get; set; }
        public string additional_image_link { get; set; }
        //public string mobile_link { get; set; }
        //public string availability { get; set; }
        //public Dictionary<string, string> availability_date { get; set; }
        //public string sale_price_effective_date { get; set; }
        //public string google_product_category { get; set; }
        //public Dictionary<string, string> product_type { get; set; }
        //public string brand { get; set; }
        //public string gtin { get; set; }
        //public string mpn { get; set; }
        //public string identifier_exists { get; set; }
        //public string adult { get; set; }
        //public string gender { get; set; }
        //public string Game { get; set; }
        //public Dictionary<string, string> Genre { get; set; }
        //public string Platform { get; set; }
        //public string Edition { get; set; }
        //public string Rating { get; set; }
        public List<Dictionary<string, string>> html_description { get; set; }
        //public Dictionary<string, string> default_price { get; set; }
        //public Dictionary<string, string> price { get; set; }
        //public Dictionary<string, string> price_range { get; set; }
        //public string merch_type { get; set; }
        //public string popularity { get; set; }
        //public int club_units { get; set; }
        //public string Novelty { get; set; }
        //public string Exclusivity { get; set; }
        //public string Free_offer { get; set; }
        //public string Searchable { get; set; }
        //public string MasterID { get; set; }
        //public string promotion_percentage { get; set; }
        //public string preorder { get; set; }
        //public string dlcType { get; set; }
        //public string release_year { get; set; }
        //public string short_title { get; set; }
        //public Dictionary<string, string> minimum_price { get; set; }
        //public string upc_included { get; set; }
        //public string linkWeb { get; set; }
        //public string linkUpc { get; set; }
        //public string platforms_availability { get; set; }
        //public string sub_brand { get; set; }
        //public int orders_data { get; set; }
        //public double revenue_data { get; set; }
        //public string comingSoon { get; set; }
        //public string Loyalty_units { get; set; }
        //public string objectID { get; set; }
        //public class _highlightResult { get; set; }
    }
}
