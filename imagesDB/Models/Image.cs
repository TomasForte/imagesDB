
namespace Models
{
    public class Image
    {
        public string Category { get; }
        public string Challenge { get; }
        public string? Difficulty { get; }
        public int Run { get; }
        public string ImageUrl { get; }
        public string? ImagePath { get; private set; }
        public string? ImageHash { get; private set; }
        public string Creator { get; }
        public string Source { get; }

        // Constructor
        public Image(string category, string challenge, string? difficulty,
                         int run, string imageUrl, string creator, string source)
        {
            Category = category;
            Challenge = challenge;
            Difficulty = difficulty;
            Run = run;
            ImageUrl = imageUrl;
            Creator = creator;
            Source = source;
        }

        public void AddPath(string imagePath)
        {
            ImagePath = imagePath;
        }

        public void AddImageHash(string imageHash)
        {
            ImageHash = imageHash;
        }

    }
}