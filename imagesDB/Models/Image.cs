
using System.Threading.Channels;
using System.Text.Json.Serialization;

namespace Models
{
    public class Image
    {
        public int? Id { get; }
        public int ChallengeId { get; }
        public string? Category { get; }
        public string? Challenge { get; }
        public string? Difficulty { get; }
        public int Run { get; }
        public string? ImageUrl { get; }
        public string? ImagePath { get; private set; }
        public string? ImageHash { get; private set; }
        public string? Creator { get; }
        public string? Source { get; }
        public string? CatboxUrl { get; set; }




        // Constructor for adding to add
        [JsonConstructor]
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

        public Image(int id, int challengeId, string imagePath)
        {
            Id = id;
            ImagePath = imagePath;
            ChallengeId = challengeId;
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