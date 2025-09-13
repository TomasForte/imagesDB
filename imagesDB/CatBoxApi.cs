
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using HtmlAgilityPack;
using Models;

public class CatboxApi
{
    private readonly HttpClient _httpClient;
    private readonly string _url;

    private readonly string _userHash;

    public CatboxApi(string userHash)
    {

        _httpClient = new HttpClient();
        _url = @"https://catbox.moe/user/api.php";
        _userHash = userHash;
        _httpClient.Timeout = TimeSpan.FromSeconds(90);
        _httpClient.DefaultRequestHeaders.ExpectContinue = false;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7.88.1");
    }

    public async Task<string> CreateAlbum(Challenge challenge)
    {
        // var values = new Dictionary<string, string>
        // {
        //     { "reqtype", "createalbum" },
        //     { "userhash", userhash },
        //     { "title", challenge.ChallengeName },
        //     { "desc", $"Category: {challenge.Category}; Challenge: {challenge.ChallengeName}; id: {challenge.Id}" }

        // };
        // var content = new FormUrlEncodedContent(values);

        var content = new MultipartFormDataContent
        {
            { new StringContent("createalbum"), "reqtype" },
            { new StringContent(_userHash), "userhash" },
            { new StringContent(challenge.ChallengeName), "title" },
            { new StringContent($"Category: {challenge.Category}; Challenge: {challenge.ChallengeName}; id: {challenge.Id}"), "desc" }
        };
        var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = content
        };
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();

        return responseString;
    }





    public async Task LoadImages(List<Image> images)
    {



        foreach (Image image in images)
        {

            using (var fileStream = File.OpenRead(image.ImagePath))
            {
                var content = new MultipartFormDataContent();
                content.Add(new StringContent("fileupload"), "reqtype");
                content.Add(new StringContent(_userHash), "userhash");
                content.Add(new StreamContent(fileStream), "fileToUpload", Path.GetFileName(image.ImagePath));

                var request = new HttpRequestMessage(HttpMethod.Post, _url)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                image.CatboxUrl = responseString;
            }

            Thread.Sleep(2000);
        }

    }
    


    public async Task AddImagesToAlbum(List<Image> images, string catboxAlbum)
    {

        string imageUrlParameter = string.Join(" ", images.Select(i => Path.GetFileName(i.CatboxUrl)));
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("addtoalbum"), "reqtype");
        content.Add(new StringContent(_userHash), "userhash");
        content.Add(new StringContent(catboxAlbum), "short");
        content.Add(new StringContent(imageUrlParameter), "files");


        var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        Console.WriteLine("OK");
            
        

    }
}