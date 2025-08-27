using Microsoft.Data.Sqlite;
using Models;

public class ImageDb
{
    private readonly string _connectionString;

    public ImageDb(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
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

    public void InsertImage(Image image)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                INSERT INTO images
                (category, challenge, difficulty, run, image_url, image_hash, creator, source)
                VALUES (
                    @category,
                    @challenge,
                    @difficulty,
                    @run,
                    @image_url,
                    @image_hash,
                    @creator,
                    @source
                )
            ;";
            command.Parameters.AddWithValue("@category", image.Category);
            command.Parameters.AddWithValue("@challenge", image.Challenge);
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
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT last_insert_rowid()";
                return (int)command.ExecuteScalar();
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
}
