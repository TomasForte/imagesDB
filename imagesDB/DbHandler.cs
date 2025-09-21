using System.Numerics;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.SqlClient;
using Models;
using System.Collections;

public class ImageDb
{
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;

    public ImageDb(string connectionString)
    {
        _connectionString = connectionString;
        _connection = new SqliteConnection(_connectionString);
    }

    public void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS challenges (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    category TEXT NOT NULL,
                    challengeName TEXT NOT NULL UNIQUE,
                    catboxAlbum TEXT,
                    ImageChestPost TEXT
                );
            ";
            command.ExecuteNonQuery();
        }


        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS images (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    challenge_id INTEGER,
                    difficulty TEXT,
                    run INTEGER,
                    image_path TEXT UNIQUE,
                    image_url TEXT UNIQUE,
                    creator TEXT,
                    source TEXT,
                    image_hash TEXT,
                    catboxUrl TEXT,
                    ImageChestUrl TEXT,
                    ImageChestPostId TEXT,
                    ImgbbUrl TEXT,
                    FOREIGN KEY (challenge_id) REFERENCES challenges(id)
                    ON DELETE CASCADE
                );
            ";
            command.ExecuteNonQuery();
        }


        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = ON;";
            command.ExecuteNonQuery();
        }

    }

    public bool ImageExists(string imageUrl)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT 1 FROM images WHERE image_url = @imageUrl";
                command.Parameters.AddWithValue("@imageUrl", imageUrl);

                using var reader = command.ExecuteReader();
                return reader.HasRows;
            }
        }
    }

    public string GetChallengeTitle(int challenge_id)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT challengeName FROM challenges WHERE id = @challengeId";
                command.Parameters.AddWithValue("@challengeId", challenge_id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetString(0); // Assumes challengeName is the first column
                    }
                    else
                    {
                        throw new InvalidOperationException($"No challenge found with ID {challenge_id}.");
                    }
                }
            }
        }
    }

    public void InsertImage(Image image)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            int challengeId;
            using (var checkCommand = connection.CreateCommand())
            {

                checkCommand.CommandText = "SELECT id FROM challenges WHERE challengeName = @challenge";
                checkCommand.Parameters.AddWithValue("@challenge", image.Challenge);

                var result = checkCommand.ExecuteScalar();

                if (result != null)
                {
                    challengeId = Convert.ToInt32(result);
                }
                else
                {
                    using (var insertChallenge = connection.CreateCommand())
                    {
                        insertChallenge.CommandText = @"
                            INSERT INTO challenges (category, challengeName)
                            VALUES (@category, @challenge);
                        ";
                        insertChallenge.Parameters.AddWithValue("@category", image.Category);
                        insertChallenge.Parameters.AddWithValue("@challenge", image.Challenge);
                        insertChallenge.ExecuteNonQuery();
                    }

                    using (var getIdCommand = connection.CreateCommand())
                    {
                        getIdCommand.CommandText = "SELECT last_insert_rowid();";
                        challengeId = Convert.ToInt32(getIdCommand.ExecuteScalar());
                    }
                }

            }



            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                INSERT INTO images
                (challenge_id, difficulty, run, image_url, image_hash, creator, source)
                VALUES (
                    @challengeId,
                    @difficulty,
                    @run,
                    @image_url,
                    @image_hash,
                    @creator,
                    @source
                )
            ;";
                command.Parameters.AddWithValue("@challengeId", challengeId);
                command.Parameters.AddWithValue("@difficulty", image.Difficulty);
                command.Parameters.AddWithValue("@run", image.Run);
                command.Parameters.AddWithValue("@image_url", image.ImageUrl);
                command.Parameters.AddWithValue("@image_hash", image.ImageHash);
                command.Parameters.AddWithValue("@creator", image.Creator);
                command.Parameters.AddWithValue("@source", image.Source);


                command.ExecuteNonQuery();

            }


        }

    }

    public int LastImageId()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT last_insert_rowid()";
                return Convert.ToInt32(command.ExecuteScalar());
            }

        }

    }

    public bool TryUpdateImagePath(string imageUrl, string imagePath)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE images SET image_path = @image_path WHERE image_url = @image_url;
            ";
            command.Parameters.AddWithValue("@image_path", imagePath);
            command.Parameters.AddWithValue("@image_url", imageUrl);


            // executeNonQuery return the number of rowsaffected
            return command.ExecuteNonQuery() > 0;
        }

    }

    public void DeleteImage(string imageUrl)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM images WHERE image_url = @imageUrl";
                command.Parameters.AddWithValue("@imageUrl", imageUrl);

                command.ExecuteNonQuery();
            }

        }

    }

    public HashSet<Challenge> GetChallengesWithoutCatboxAlbum()
    {
        HashSet<Challenge> newChallenges = new HashSet<Challenge>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, category, challengeName FROM challenges WHERE catboxAlbum IS NULL";

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        newChallenges.Add(new Challenge(
                            reader.GetInt32(0),
                            reader.GetString(1),
                            reader.GetString(2)
                        ));
                    }
                }
            }

        }
        return newChallenges;
    }


    public HashSet<Challenge> GetChallengesWithoutImageChestPost()
    {
        HashSet<Challenge> newChallenges = new HashSet<Challenge>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, category, challengeName FROM challenges WHERE ImageChestPost IS NULL";

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        newChallenges.Add(new Challenge(
                            reader.GetInt32(0),
                            reader.GetString(1),
                            reader.GetString(2)
                        ));
                    }
                }
            }

        }
        return newChallenges;
    }


    public void UpdateChallengeCatboxAlbum(int challengeId, string PostId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE challenges SET ImageChestPost = @PostId WHERE id = @challengeId;";
                command.Parameters.AddWithValue("@PostId", PostId);
                command.Parameters.AddWithValue("@challengeId", challengeId);

                command.ExecuteNonQuery();
            }
        }
    }


        public void UpdateChallengeImageChestPost(int challengeId, string albumCode)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE challenges SET catboxAlbum = @albumCode WHERE id = @challengeId;";
                command.Parameters.AddWithValue("@albumCode", albumCode);
                command.Parameters.AddWithValue("@challengeId", challengeId);

                command.ExecuteNonQuery();
            }
        }
    }
    public Dictionary<int, List<Image>> ImagesNotInImgbb()
    {
        Dictionary<int, List<Image>> newImages = new Dictionary<int, List<Image>>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, Challenge_id, image_path 
                                        FROM images 
                                        WHERE ImgbbUrl IS NULL";

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    int challengeId;
                    while (reader.Read())
                    {
                        challengeId = reader.GetInt32(1);
                        newImages.TryAdd(challengeId, new List<Image>());
                        newImages[challengeId].Add(
                            new Image(
                                id: reader.GetInt32(0),
                                challengeId: reader.GetInt32(1),
                                imagePath : reader.GetString(2)
                                )
                        );
                    }
                }
            }

        }
        return newImages;
    }

    public Dictionary<int, List<Image>> ImagesNotInCatbox()
    {
        Dictionary<int, List<Image>> newImages = new Dictionary<int, List<Image>>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, Challenge_id, image_path 
                                        FROM images 
                                        WHERE catboxUrl IS NULL";

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    int challengeId;
                    while (reader.Read())
                    {
                        challengeId = reader.GetInt32(1);
                        newImages.TryAdd(challengeId, new List<Image>());
                        newImages[challengeId].Add(
                            new Image(
                                id: reader.GetInt32(0),
                                challengeId: reader.GetInt32(1),
                                imagePath : reader.GetString(2)
                                )
                        );
                    }
                }
            }

        }
        return newImages;
    }

    public List<Image> GetImagesInImageChest()
    {
        List<Image> ImagesInImageChest = new List<Image>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, Challenge_id, ImageChestUrl 
                                        FROM images 
                                        WHERE ImageChestUrl != ''";

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    int challengeId;
                    while (reader.Read())
                    {
                        ImagesInImageChest.Add(
                            new Image(
                                id: reader.GetInt32(0),
                                challengeId: reader.GetInt32(1),
                                imageChestUrl : reader.GetString(2)
                                )
                        );
                    }
                }
            }

        }
        return ImagesInImageChest;
    }


        public Dictionary<int, List<Image>> ImagesNotInImageChest()
    {
        Dictionary<int, List<Image>> newImages = new Dictionary<int, List<Image>>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, Challenge_id, image_path, difficulty, run 
                                        FROM images 
                                        WHERE ImageChestUrl IS NULL";

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    int challengeId;
                    while (reader.Read())
                    {
                        challengeId = reader.GetInt32(1);
                        newImages.TryAdd(challengeId, new List<Image>());
                        newImages[challengeId].Add(
                            new Image(
                                id: reader.GetInt32(0),
                                challengeId: reader.GetInt32(1),
                                imagePath : reader.GetString(2),
                                difficulty: reader.GetString(3),
                                run: reader.GetInt32(4)
                                )
                        );
                    }
                }
            }

        }
        return newImages;
    }
    public string GetChallengeImageChestPost(int challengeId)
    {
        string ChallengePost;

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT ImageChestPost FROM challenges WHERE id = @challengeId;";
                command.Parameters.AddWithValue("@challengeId", challengeId);

                var result = command.ExecuteScalar();

                try
                {
                    ChallengePost = Convert.ToString(result);
                }
                catch
                {
                    throw;
                }
            }

        }
        return ChallengePost;
    }


    public string GetChallengeCatboxAlbum(int challengeId)
    {
        string challengeAlbum;

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT catboxAlbum FROM challenges WHERE id = @challengeId;";
                command.Parameters.AddWithValue("@challengeId", challengeId);

                var result = command.ExecuteScalar();

                try
                {
                    challengeAlbum = Convert.ToString(result);
                }
                catch
                {
                    throw;
                }
            }

        }
        return challengeAlbum;
    }


    public void UpdateImageImgbbUrl(Image image, string imgbbUrl)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE images SET ImgbbUrl = @imgbbImageUrl WHERE id = @imageId;";
                command.Parameters.AddWithValue("@imageId", image.Id);
                command.Parameters.AddWithValue("@imgbbImageUrl", imgbbUrl);

                command.ExecuteNonQuery();
            }
        }
    }

    public void UpdateImagesCatBoxUrl(Image image)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE images SET catboxUrl = @catboxImageUrl WHERE id = @imageId;";
                command.Parameters.AddWithValue("@imageId", image.Id);
                command.Parameters.AddWithValue("@catboxImageUrl", image.CatboxUrl);

                command.ExecuteNonQuery();
            }
        }
    }


    public void UpdateImagesImageChestUrl(int imageId, string imageChestUrl, string imageChestPostId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"UPDATE images 
                                        SET 
                                            ImageChestUrl = @ImageChestUrl, 
                                            ImageChestPostId = @ImageChestPostId
                                        WHERE id = @imageId;";
                command.Parameters.AddWithValue("@imageId", imageId);
                command.Parameters.AddWithValue("@ImageChestUrl", imageChestUrl);
                command.Parameters.AddWithValue("@ImageChestPostId", imageChestPostId);

                command.ExecuteNonQuery();
            }
        }
    }
}
