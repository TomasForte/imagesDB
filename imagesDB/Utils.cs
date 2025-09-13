
using System.Security.Cryptography;
using Models;
using System.Net;
using HtmlAgilityPack;

class Utils
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

    public static String FindSolutionDirectoryDirectory()
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
            Environment.Exit(1);
        }

        return dir.FullName;
    }

    public static string GetImageHash(Byte[] imageBytes)
    {
        byte[] hashBytes;
        using var sha256 = SHA256.Create();
        {
            hashBytes = sha256.ComputeHash(imageBytes);
        }
        string hashHexString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        return hashHexString;
    }

    public static HtmlNodeCollection GetTableContent(string htmlContent )
    {
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
        return rows;
        
    }

    public static List<Image> GetImages(HtmlNodeCollection tableRows)
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