using System;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

using Models;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;




namespace MyConsoleApp
{

    class Program
    {

        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            string connectionString = "Data Source=images.db";
            ImageDb dbHandler = new ImageDb(connectionString);
            dbHandler.InitializeDatabase();

            string url = config["Scraping:BadgeUrl"] ?? throw new InvalidOperationException("Badge URL not found.");
            string catBoxUserHash = config["Scraping:CatBoxUserHash"] ?? throw new InvalidOperationException("Badge URL not found.");
            string ImageChestApiToken = config["Scraping:ImageChestApiToken"] ?? throw new InvalidOperationException("ImageChestApiToken not found");
            string imgbbKey = config["Scraping:imgbbKey"] ?? throw new InvalidOperationException("imgbbKey not found");
            string jsonFile = config["JsonFile"] ?? throw new InvalidOperationException("Json File not found.");


            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var slnFile = dir.GetFiles("*.sln").FirstOrDefault();
                if (slnFile != null)
                    break;

                dir = dir.Parent;
            }
            if (dir == null)
            {
                throw new InvalidOperationException("Solution directory not found.");
            }
                
            jsonFile = Path.Combine(dir.FullName, jsonFile);

            var catboxApi = new CatboxApi(catBoxUserHash);
            var imageChestApi = new ImageChestApi(ImageChestApiToken);
            var imgbbApi = new ImgbbApi(imgbbKey);
            var imageService = new ImageService(dbHandler, catboxApi, imageChestApi, imgbbApi);


            try
            {
                await imageService.DownloadNewImages(url);
                await imageService.DownloadNewImagesFromJson(jsonFile);
                await imageService.LoadImagesToCatbox();
                await imageService.LoadImagesToImageChest();
                //await imageService.LoadImagesToImgbb();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
            }
        }
        
    }
}