using System.Text.Json.Serialization;

namespace Models
{
    public class ImgbbResponse
    {
        [JsonPropertyName("data")]
        public ImgbbData Data { get; set; }
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }
    }

    public class ImgbbData
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("display_url")]
        public string DisplayUrl { get; set; }

    }
}