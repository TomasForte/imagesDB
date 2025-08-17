using System;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Models;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;

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

        static async Task Main(string[] args)
        {
            List<Image> images = new List<Image>();
            string connectionString = "Data Source=images.db";
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
                Console.WriteLine("base path not found");
                return;
            }
            
            // Making http request
            using var client = new HttpClient();
            string htmlContent = "";
            try
            {
                string url = "https://anime.jhiday.net/hof/badges";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Throws if not 200–299

                htmlContent = await response.Content.ReadAsStringAsync();

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
                return;
            }

            // loop to rows in table and create images
            foreach (var row in rows)
            {
                HtmlNodeCollection cells = row.SelectNodes(".//td");
                if (cells != null)
                {
                    HtmlNode imageLinkNode = cells[4].SelectSingleNode(".//a");
                    string imageUrl = imageLinkNode?.GetAttributeValue("href", "") ?? "";

                    images.Add(new Image(
                        category: cells[0].InnerText.Trim(),
                        challenge: cells[1].InnerText.Trim(),
                        difficulty: cells[2].InnerText.Trim(),
                        run: Convert.ToInt32(cells[3].InnerText.Trim()),
                        imageUrl: imageUrl,
                        creator: cells[5].InnerText.Trim(),
                        source: cells[6].InnerText.Trim()
                        )
                    );
                }
            }


            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var createTableCommand = connection.CreateCommand();

                createTableCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS images (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        category TEXT NOT NULL,
                        challenge TEXT NOT NULL,
                        difficulty TEXT,
                        run INTEGER,
                        image_path TEXT UNIQUE,
                        image_url TEXT UNIQUE,
                        creator TEXT,
                        source TEXT
                    );
                ";
                createTableCommand.ExecuteNonQuery();
            
                foreach (Image image in images)
                {
                    // check if image is already in db
                    var ImageExistCommand = connection.CreateCommand();
                    ImageExistCommand.CommandText = @"
                        SELECT * FROM images
                        WHERE image_url = @imageUrl
                    ";
                    ImageExistCommand.Parameters.AddWithValue("@imageUrl", image.ImageUrl);



                    using (var reader = ImageExistCommand.ExecuteReader())
                    {
                        // if image not in db download it and insert path into db
                        if (!reader.HasRows)
                        {
                            // define dir when image will be stored
                            string imageDir = Path.Combine(dir.FullName, "Images", SanitizeFileName(image.Category), SanitizeFileName(image.Challenge));
                            Directory.CreateDirectory(imageDir);


                            // request imageurl
                            byte[] imageBytes = Array.Empty<byte>();;
                            string contentType = "application/octet-stream";
                            try
                            {
                                HttpResponseMessage response = await client.GetAsync(image.ImageUrl);
                                response.EnsureSuccessStatusCode(); // Throws if not 200–299

                                imageBytes =  await response.Content.ReadAsByteArrayAsync();
                                contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                            }
                            catch (HttpRequestException e)
                            {
                                Console.WriteLine($"Request error: {e.Message}");
                                Console.WriteLine($"Image  {image.ImageUrl} could not be downloaded");
                                Environment.Exit(1);
                            }
                            
                            // get file extension of the image
                            string extension = contentType switch
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
                                return;
                            }

                            // insert image to DB
                            var addImageCommand = connection.CreateCommand();
                            addImageCommand.CommandText = @"
                                INSERT INTO images
                                (category, challenge, difficulty, run, image_url,   creator, source)
                                VALUES (
                                    @category,
                                    @challenge,
                                    @difficulty,
                                    @run,
                                    @image_url,
                                    @creator,
                                    @source
                                )
                            ;";
                            addImageCommand.Parameters.AddWithValue("@category", image.Category);
                            addImageCommand.Parameters.AddWithValue("@challenge", image.Challenge);
                            addImageCommand.Parameters.AddWithValue("@difficulty", image.Difficulty);
                            addImageCommand.Parameters.AddWithValue("@run", image.Run);
                            addImageCommand.Parameters.AddWithValue("@image_url", image.ImageUrl);
                            addImageCommand.Parameters.AddWithValue("@creator", image.Creator);
                            addImageCommand.Parameters.AddWithValue("@source", image.Source);


                            addImageCommand.ExecuteNonQuery();

                            // get id of the last image
                            var lastIdCommand = connection.CreateCommand();
                            lastIdCommand.CommandText = "SELECT last_insert_rowid()";
                            long lastId = (long)lastIdCommand.ExecuteScalar();

                            // define imageName and imagePath to store the image
                            string imageName = SanitizeFileName($@"{lastId}_{image.Challenge}_{image.Difficulty}_{image.Run}{extension}");
                            string imagePath = Path.Combine(imageDir, imageName);

                            image.AddPath(imagePath);

                            try
                            {
                                await File.WriteAllBytesAsync(imagePath, imageBytes);
                            }
                            catch
                            {

                                // remove image from db if storing image in disk fails
                                var deleteImageCommand = connection.CreateCommand();
                                deleteImageCommand.CommandText = @"
                                    DELETE FROM images WHERE image_url = @imageUrl
                                ;";
                                deleteImageCommand.Parameters.AddWithValue("@image_url", image.ImageUrl);
                                deleteImageCommand.ExecuteNonQuery();
                                return;
                            }

                            // add the imagePath to the database
                            var updateImagePathCommand = connection.CreateCommand();
                            updateImagePathCommand.CommandText = @"
                                    UPDATE images SET image_path = @image_path
                                    WHERE image_url = @image_url 
                                ;";
                            updateImagePathCommand.Parameters.AddWithValue("@image_path", image.ImagePath);
                            updateImagePathCommand.Parameters.AddWithValue("@image_url", image.ImageUrl);


                            if (!(updateImagePathCommand.ExecuteNonQuery() > 0))
                            {
                                // delete image from db as well as the stored file if the insert of imagePath to db fails
                                File.Delete(imagePath);
                                var deleteImageCommand = connection.CreateCommand();
                                deleteImageCommand.CommandText = @"
                                    DELETE FROM images WHERE image_url = @imageUrl
                                ;";
                                deleteImageCommand.Parameters.AddWithValue("@image_url", image.ImageUrl);
                                deleteImageCommand.ExecuteNonQuery();

                                return;
                            }

                        }
                    }
                }
            }
        }
    }
}