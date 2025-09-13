
public class HttpService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private bool _disposed = false;


    public static async Task<string> RequestHtmlContent(string url)
    {
        string htmlContent;
    
        HttpResponseMessage response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Throws if not 200–299

        htmlContent = await response.Content.ReadAsStringAsync();

            
        
        return htmlContent;


    }

    public static async Task<(byte[] ImageBytes, string Extension)> DownloadImageAsync(string imageUrl)
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


}