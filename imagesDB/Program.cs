using System;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Models;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;


namespace MyConsoleApp
{

    class Program
    {

        //remove character that break a path
        public static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '-');
            }
            return fileName;
        }



        public static async Task LoadImagesToCatbox(ImageDb dbHandler)
        {
            var newChallenges = dbHandler.GetNewChallenges();


        }

        public static async Task LoadNewImages(ImageDb dbHandler, string url)
        {


            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            // start in the folder with the exe and go down until i find the dir with the solution file
            while (dir != null)
            {
                var slnFile = dir.GetFiles("*.sln").FirstOrDefault();
                if (slnFile != null)
                    break;

                dir = dir.Parent;
            }

            if (dir == null)
            {
                Console.WriteLine("base path not found");
                return;
            }

            // Making http request

            HttpService service = new HttpService();
            string htmlContent = "";

            try
            {
                htmlContent = await service.RequestHtmlContent(url);

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                Environment.Exit(1);
            }

            // load html to parse
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);
            var tables = htmlDoc.DocumentNode.SelectNodes("//table");

            if (tables == null || tables.Count != 1)
            {
                throw new InvalidOperationException("HTML must contain exactly one table element.");
            }


            HtmlNodeCollection rows = tables[0].SelectNodes(".//tr");
            if (rows == null)
            {
                Console.WriteLine("No rows found in the table.");
                throw new InvalidOperationException("Tables must have rows");
            }

            // loop to rows in table and create images
            List<Image> images = service.GetImages(rows);


            /*-----------------------TODO ----------------------------------------*/
            /*To prevent having to insert and then remove imagens from db if sth fails
            I should make a transation so that if sth fails i roll the entire process back*/

            foreach (Image image in images)
            {

                // if image in db go to next
                if (dbHandler.ImageExists(image.ImageUrl))
                {
                    continue;
                }

                // define dir where image will be stored
                string imageDir = Path.Combine(dir.FullName, "Images", SanitizeFileName(image.Category), SanitizeFileName(image.Challenge));
                Directory.CreateDirectory(imageDir);


                // request imageurl
                byte[] imageBytes = Array.Empty<byte>(); ;
                string imageExtension = "bin";
                try
                {
                    var (bytes, extension) = await service.DownloadImageAsync(image.ImageUrl);

                    imageBytes = bytes;
                    imageExtension = extension;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                    Console.WriteLine($"Image  {image.ImageUrl} could not be downloaded");
                    Environment.Exit(1);
                }

                // Images Hash
                byte[] hashBytes;
                using var sha256 = SHA256.Create();
                {
                    hashBytes = sha256.ComputeHash(imageBytes);
                }
                string hashHexString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                image.AddImageHash(hashHexString);




                // insert image to DB
                try
                {
                    dbHandler.InsertImage(image);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to add Image to db: {image.ImageUrl} — {ex.Message}");
                    Environment.Exit(1);
                }

                // define imageName and imagePath to store the image
                int lastId = dbHandler.LastImageId();
                string imageName = SanitizeFileName($@"{lastId}_{image.Challenge}_{image.Difficulty}_{image.Run}{imageExtension}");
                string imagePath = Path.Combine(imageDir, imageName);


                image.AddPath(imagePath);



                try
                {
                    await File.WriteAllBytesAsync(imagePath, imageBytes);
                }
                catch (Exception ex)
                {

                    // remove image from db if storing image in disk fails
                    dbHandler.DeleteImage(image.ImageUrl);
                    Console.WriteLine($"Failed to write image to disk: {imagePath} — {ex.Message}");
                    return;
                }

                // add the imagePath to the database
                if (!dbHandler.TryUpdateImagePath(image.ImageUrl, imagePath))
                {
                    // delete image from db as well as the stored file if the insert of imagePath to db fails
                    File.Delete(imagePath);
                    // remove image from db if storing image in disk fails
                    dbHandler.DeleteImage(image.ImageUrl);

                    return;
                }

            }
        }


        static async Task Main(string[] args)
        {

            string connectionString = "Data Source=images.db";
            ImageDb dbHandler = new ImageDb(connectionString);
            dbHandler.InitializeDatabase();
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            string url = config["Scraping:BadgeUrl"] ?? throw new InvalidOperationException("Badge URL not found.");
            string catBoxUserHash = config["Scraping:CatBoxUserHash"] ?? throw new InvalidOperationException("Badge URL not found.");

            await LoadNewImages(dbHandler, url);

            

            await LoadImagesToCatbox(dbHandler);
        }
        
    }
}