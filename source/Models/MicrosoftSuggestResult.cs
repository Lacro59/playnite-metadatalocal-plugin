using System.Collections.Generic;

namespace MetadataLocal.Models
{
    public class MicrosoftSuggestResult
    {
        public string Query { get; set; }
        public List<ResultSet> ResultSets { get; set; }
        public List<ErrorSet> ErrorSets { get; set; }
    }

    public class Meta
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class Suggest
    {
        public string Source { get; set; }
        public string Title { get; set; }
        public object Description { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public List<Meta> Metas { get; set; }
        public bool Curated { get; set; }
    }

    public class ResultSet
    {
        public string Source { get; set; }
        public bool FromCache { get; set; }
        public string Type { get; set; }
        public List<Suggest> Suggests { get; set; }
        public object Metas { get; set; }
    }

    public class ErrorSet
    {
        public string Source { get; set; }
        public string Message { get; set; }
    }
}
