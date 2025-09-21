
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using HtmlAgilityPack;
using Models;
using System.Text;
using System.Text.Json;

public class ImageChestApi
{
    private readonly HttpClient _httpClient;
    private readonly string _url;

    private readonly string _userHash;

    public ImageChestApi(string ApiToken)
    {

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.imgchest.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiToken);

    }

    public async Task<ImageChestUploadResponseData> CreatePost(string challengeTitle, Image image, FileStream imageStream)
    {
        var content = new MultipartFormDataContent();

        content.Add(new StringContent(@$"{challengeTitle}_{image.Difficulty}_{image.Run}_{image.Id}"), "title");

        var fileContent = new StreamContent(imageStream);
        content.Add(fileContent, "images[]", Path.GetFileName(image.ImagePath));


        var response = await _httpClient.PostAsync("post", content);
        var responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"API call failed: {response.StatusCode}\n{responseString}");
        }
        var result = JsonSerializer.Deserialize<ImageChestUploadResponse>(responseString);


        return result.Data;
    }




    public async Task DeleteFile(string fileId)
    {

        var requestUri = new Uri($"file/{fileId}", UriKind.Relative);
        var response = await _httpClient.DeleteAsync(requestUri);
        var responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"API call failed: {response.StatusCode}\n{responseString}");
        }
    }

    public async Task DeletePost(string postId)
    {

        
        var response = await _httpClient.DeleteAsync($"post/{postId}");
        var responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"API call failed: {response.StatusCode}\n{responseString}");
        }
    }


}