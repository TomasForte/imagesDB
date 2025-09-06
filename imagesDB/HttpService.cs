
using System.Net;
using HtmlAgilityPack;
using Models;

public class HttpService
{
    private readonly HttpClient _httpClient;

    public HttpService()
    {

        _httpClient = new HttpClient();
    }


    public async Task<string> RequestHtmlContent(string url)
    {
        string htmlContent;

        HttpResponseMessage response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Throws if not 200–299

        htmlContent = await response.Content.ReadAsStringAsync();

            
        
        return htmlContent;


    }

    public async Task<(byte[] ImageBytes, string Extension)> DownloadImageAsync(string imageUrl)
    {
        byte[] imageBytes = Array.Empty<byte>();
        string extension = ".bin";

        HttpResponseMessage response = await _httpClient.GetAsync(imageUrl);
        response.EnsureSuccessStatusCode(); // Throws if not 200–299

        imageBytes = await response.Content.ReadAsByteArrayAsync();
        string contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        // get file extension of the image
        extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".bin" // fallback
        };

        if (extension == ".bin")
        {
            Console.WriteLine("file extension not found");
        }
        
        return (imageBytes, extension);
    }

    public List<Image> GetImages(HtmlNodeCollection tableRows)
    {
        List<Image> images = new List<Image>();
        foreach (var row in tableRows)
        {
            HtmlNodeCollection cells = row.SelectNodes(".//td");
            if (cells != null)
            {
                HtmlNode imageLinkNode = cells[4].SelectSingleNode(".//a");
                string imageUrl = imageLinkNode?.GetAttributeValue("href", "") ?? "";

                images.Add(new Image(
                    category: WebUtility.HtmlDecode(cells[0].InnerText.Trim()),
                    challenge: WebUtility.HtmlDecode(cells[1].InnerText.Trim()),
                    difficulty: WebUtility.HtmlDecode(cells[2].InnerText.Trim()),
                    run: Convert.ToInt32(cells[3].InnerText.Trim()),
                    imageUrl: imageUrl,
                    creator: cells[5].InnerText.Trim(),
                    source: cells[6].InnerText.Trim()
                    )
                );
            }
        }

        return images;
    }
}