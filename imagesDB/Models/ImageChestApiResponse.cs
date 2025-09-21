using System.Text.Json.Serialization;

namespace Models
{
    public class ImageChestImage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("link")]
        public string Link { get; set; }
        [JsonPropertyName("position")]
        public int Position { get; set; }
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }
    }

    public class ImageChestUploadResponseData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; }
        [JsonPropertyName("privacy")]
        public string Privacy { get; set; }
        [JsonPropertyName("report_status")]
        public int ReportStatus { get; set; }
        [JsonPropertyName("views")]
        public int Views { get; set; }
        [JsonPropertyName("nsfw")]
        public int Nsfw { get; set; }
        [JsonPropertyName("image_count")]
        public int ImageCount { get; set; }
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }
        [JsonPropertyName("images")]
        public List<ImageChestImage> Images { get; set; }
    }

    public class ImageChestUploadResponse
    {
        [JsonPropertyName("data")]
        public ImageChestUploadResponseData Data { get; set; }
    }
}