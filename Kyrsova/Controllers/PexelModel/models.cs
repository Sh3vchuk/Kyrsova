using System;
using Newtonsoft.Json;

namespace Kyrsova.Models
{
    public class PexelsSearch
    {
        [JsonProperty("page")]
        public long Page { get; set; }

        [JsonProperty("per_page")]
        public long PerPage { get; set; }

        [JsonProperty("photos")]
        public Photo[] Photos { get; set; }

        [JsonProperty("total_results")]
        public long TotalResults { get; set; }

        [JsonProperty("next_page")]
        public Uri NextPage { get; set; }
    }

    public class Photo
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("photographer")]
        public string Photographer { get; set; }

        [JsonProperty("photographer_url")]
        public Uri PhotographerUrl { get; set; }

        [JsonProperty("photographer_id")]
        public long PhotographerId { get; set; }

        [JsonProperty("avg_color")]
        public string AvgColor { get; set; }

        [JsonProperty("src")]
        public Src Src { get; set; }

        [JsonProperty("liked")]
        public bool Liked { get; set; }

        [JsonProperty("alt")]
        public string Alt { get; set; }
    }

    public class Src
    {
        [JsonProperty("original")]
        public Uri Original { get; set; }

        [JsonProperty("large2x")]
        public Uri Large2X { get; set; }

        [JsonProperty("large")]
        public Uri Large { get; set; }

        [JsonProperty("medium")]
        public Uri Medium { get; set; }

        [JsonProperty("small")]
        public Uri Small { get; set; }

        [JsonProperty("portrait")]
        public Uri Portrait { get; set; }

        [JsonProperty("landscape")]
        public Uri Landscape { get; set; }

        [JsonProperty("tiny")]
        public Uri Tiny { get; set; }
    }
}