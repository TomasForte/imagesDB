
using System.ComponentModel.DataAnnotations;
using HtmlAgilityPack;
using Models;

public class CatboxApi
{
    private readonly HttpClient _httpClient;
    private readonly string _url;

    public CatboxApi()
    {

        _httpClient = new HttpClient();
        _url = @"https://catbox.moe/user/api.php";
    }

    public async void CreateAlbum(string userhash, string title)
    {
        var values = new Dictionary<string, string>
        {
            { "reqtype", "createalbum" },
            { "userhash", userhash },
            { "title", title }
        };
        var content = new FormUrlEncodedContent(values);
        var response = await _httpClient.PostAsync(_url, content);
        var responseString = await response.Content.ReadAsStringAsync();
    }
}