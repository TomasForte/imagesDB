
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using HtmlAgilityPack;
using Models;
using System.Text;
using System.Text.Json;

public class ImgbbApi
{
    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly string _apiKey;
    private readonly string _userHash;

    public ImgbbApi(string apiKey)
    {

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.imgbb.com/");
        _apiKey = apiKey;
    }

    public async Task<string> UploadImage(Image image)
    {


        using (var fileStream = File.OpenRead(image.ImagePath))
        {
            var content = new MultipartFormDataContent
            {
                { new StreamContent(fileStream), "image", Path.GetFileName(image.ImagePath) }
            };

            var queryParams = new Dictionary<string, string>
            {
                { "expiration", "600" },
                { "key", _apiKey }
            };
            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var endpoint = $"1/upload?{queryString}";

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Upload failed: {response.StatusCode}\n{responseString}");
            }
            var result = JsonSerializer.Deserialize<ImgbbResponse>(responseString);
            if (result?.Data?.Url == null)
            {
                throw new Exception("Upload succeeded but no image URL was returned.");
            }

            return result.Data.Url;

        }
    }

}