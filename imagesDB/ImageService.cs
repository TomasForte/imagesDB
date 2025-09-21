using Models;
using HtmlAgilityPack;
using System.Text;
using System.Text.Json;

public class ImageService
{
    private readonly ImageDb _dbHandler;
    private readonly CatboxApi _catboxApi;
    private readonly ImageChestApi _imageChestApi;
    private readonly ImgbbApi _imgbbApi;

    public ImageService(ImageDb dbHandler, CatboxApi catboxApi, ImageChestApi imageChestApi, ImgbbApi imgbbApi)
    {
        _dbHandler = dbHandler;
        _catboxApi = catboxApi;
        _imageChestApi = imageChestApi;
        _imgbbApi = imgbbApi;
    }



    public async Task DownloadNewImages(string url)
    {

        string BaseDirectory = Utils.FindSolutionDirectoryDirectory();



        List<Image> images = await GetImagesFromIndex(url);



        /*-----------------------TODO ----------------------------------------*/
        /*To prevent having to insert and then remove imagens from db if sth fails
        I should make a transation so that if sth fails i roll the entire process back*/

        foreach (Image image in images)
        {

            // if image in db go to next
            if (_dbHandler.ImageExists(image.ImageUrl))
            {
                continue;
            }

            await ProcessAndStoreImage(BaseDirectory, image);


        }
    }


    public async Task<List<Image>> GetImagesFromIndex(string url)
    {
        // Making http request
        string htmlContent = "";

        try
        {
            htmlContent = await HttpService.RequestHtmlContent(url);

        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            Environment.Exit(1);
        }

        var tableContent = Utils.GetTableContent(htmlContent);


        return Utils.GetImages(tableContent);
    }



    public async Task DownloadNewImagesFromJson(string jsonFile)
    {
        string BaseDirectory = Utils.FindSolutionDirectoryDirectory();
        List<Image> images;

        try
        {
            string jsonContent = File.ReadAllText(jsonFile);
            images = JsonSerializer.Deserialize<List<Image>>(jsonContent) ?? new List<Image>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read or parse JSON file: {ex.Message}");
            return;
        }

        foreach (Image image in images)
        {

            // if image in db go to next
            if (_dbHandler.ImageExists(image.ImageUrl))
            {
                continue;
            }

            await ProcessAndStoreImage(BaseDirectory, image);


        }


    }


    public async Task ProcessAndStoreImage(string baseDir, Image image)
    {
        string imageDir = CreateImageDirectory(baseDir, image);
        var (imageBytes, imageExtension) = await TryDownloadImage(image.ImageUrl);
        if (imageBytes == null) return;

        string imageHash = Utils.GetImageHash(imageBytes);
        image.AddImageHash(imageHash);

        if (!TryInsertImage(image)) return;

        int lastId = _dbHandler.LastImageId();
        string imageName = Utils.SanitizeFileName($@"{lastId}_{image.Challenge}_{image.Difficulty}_{image.Run}{imageExtension}");
        string imagePath = Path.Combine(imageDir, imageName);
        image.AddPath(imagePath);

        if (!await TryWriteImageToDisk(imagePath, imageBytes, image.ImageUrl)) return;
        if (!TryUpdateImagePath(image.ImageUrl, imagePath)) return;

    }
    
    public async Task LoadImagesToImgbb()
    {

        Dictionary<int, List<Image>> newImages;

        newImages = _dbHandler.ImagesNotInImgbb();
        string challengeAlbum;
        string imageUrl;

        foreach (int challengeId in newImages.Keys)
        {

            challengeAlbum = _dbHandler.GetChallengeCatboxAlbum(challengeId);
            Console.WriteLine($"Loading images to album {challengeId})");
            // Load images to catbox and add url to image property
            foreach (Image image in newImages[challengeId])
            {
                imageUrl = await _imgbbApi.UploadImage(image);

                _dbHandler.UpdateImageImgbbUrl(image, imageUrl);
                await  Task.Delay(2000);
            }
        }
    }



    
    // this is only for catbox
    public async Task CreateAlbumForNewChallenge()
    {
        var newChallenges = _dbHandler.GetChallengesWithoutCatboxAlbum();
        foreach (var challenge in newChallenges)
        {
            string result = "";
            string albumCode;
            // wait to avoid problems with the requests

            Thread.Sleep(3000);
            try
            {
                result = await _catboxApi.CreateAlbum(challenge);
                Console.WriteLine($"Album created for '{challenge.ChallengeName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to creat catboxAlbum: {ex.Message}");
                Environment.Exit(1);
            }

            if (!string.IsNullOrEmpty(result))
            {
                string[] parts = result.Split('/');
                albumCode = parts[^1];
                _dbHandler.UpdateChallengeCatboxAlbum(challenge.Id, albumCode);
            }
        }
    }

    public async Task LoadImagesToCatbox()
    {
        await CreateAlbumForNewChallenge();

        Dictionary<int, List<Image>> newImages;

        newImages = _dbHandler.ImagesNotInCatbox();
        string challengeAlbum;

        foreach (int challengeId in newImages.Keys)
        {

            challengeAlbum = _dbHandler.GetChallengeCatboxAlbum(challengeId);
            Console.WriteLine($"Loading images to album {challengeId} - {challengeAlbum}");
            // Load images to catbox and add url to image property
            await _catboxApi.LoadImages(newImages[challengeId]);
            Console.WriteLine($"Adding catboxurl to db");
            foreach (Image image in newImages[challengeId])
            {
                _dbHandler.UpdateImagesCatBoxUrl(image);
            }

            Console.WriteLine($"add imaesto catbox album");
            await _catboxApi.AddImagesToAlbum(newImages[challengeId], challengeAlbum);
        }
    }

    

    public string CreateImageDirectory(string BaseDirectory, Image image)
    {
        // define dir where image will be stored
        string imageDir = Path.Combine(BaseDirectory, "Images", Utils.SanitizeFileName(image.Category), Utils.SanitizeFileName(image.Challenge));
        Directory.CreateDirectory(imageDir);
        return imageDir;
    }

    
    public async Task DeleteImageChestImages()
    {
        List<Image> ImagesUploadedToImageChest;

        ImagesUploadedToImageChest = _dbHandler.GetImagesInImageChest();
        string challengePost;
        string challengeTitle;

        foreach (Image image in ImagesUploadedToImageChest)
        {

            Uri uri = new Uri(image.ImageChestUrl);
            string fileName = Path.GetFileName(uri.AbsolutePath);
            string id = fileName.Split('.')[0];

            await _imageChestApi.DeleteFile(id);
            Thread.Sleep(1000);

        }
    }


    public async Task LoadImagesToImageChest()
    {
        Dictionary<int, List<Image>> newImages;

        newImages = _dbHandler.ImagesNotInImageChest();
        string challengePost;
        string challengeTitle;

        foreach (int challengeId in newImages.Keys)
        {
            var startTime = DateTime.UtcNow;
            challengePost = _dbHandler.GetChallengeImageChestPost(challengeId);
            Console.WriteLine($"Loading images to album {challengeId}");
            challengeTitle = _dbHandler.GetChallengeTitle(challengeId);

            foreach (Image image in newImages[challengeId])
            {
                using (var imageStream = File.OpenRead(image.ImagePath))
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    var remainingDelay = TimeSpan.FromSeconds(1) - elapsed;
                    if (remainingDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(remainingDelay);
                    }
                    var apiUploadResponse = await _imageChestApi.CreatePost(challengeTitle, image, imageStream);
                    startTime = DateTime.UtcNow;

                    try
                    {
                        _dbHandler.UpdateImagesImageChestUrl(image.Id, apiUploadResponse.Images[0].Link, apiUploadResponse.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Failed to update image {image.Id}: {ex.Message}");
                        await _imageChestApi.DeletePost(apiUploadResponse.Id);
                    }
                }
            }


        }
    }





    private async Task<(byte[]?, string?)> TryDownloadImage(string imageUrl)
    {
        try
        {
            return await HttpService.DownloadImageAsync(imageUrl);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            Console.WriteLine($"Image {imageUrl} could not be downloaded");
            return (null, null);
        }
    }


    private bool TryInsertImage(Image image)
    {
        try
        {
            _dbHandler.InsertImage(image);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add Image to db: {image.ImageUrl} — {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TryWriteImageToDisk(string imagePath, byte[] imageBytes, string imageUrl)
    {
        try
        {
            await File.WriteAllBytesAsync(imagePath, imageBytes);
            return true;
        }
        catch (Exception ex)
        {
            _dbHandler.DeleteImage(imageUrl);
            Console.WriteLine($"Failed to write image to disk: {imagePath} — {ex.Message}");
            return false;
        }
    }

    private bool TryUpdateImagePath(string imageUrl, string imagePath)
    {
        if (!_dbHandler.TryUpdateImagePath(imageUrl, imagePath))
        {
            File.Delete(imagePath);
            _dbHandler.DeleteImage(imageUrl);
            return false;
        }
        return true;
    }

}